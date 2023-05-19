using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class PlayableDirectorExtensions {
  static void ProcessTrackMarkers(
  this PlayableDirector director,
  IMarker marker,
  PlayableOutput output,
  double oldTime,
  double time) {
    if (!(marker is Marker))
      return;
    if (!(marker is INotification))
      return;
    bool fire = (marker.time >= oldTime && marker.time < time) || (marker.time > time && marker.time <= oldTime);
    if (fire) {
      output.PushNotification(output.GetSourcePlayable(), marker as INotification);
    }
  }

  static void FireMarkersManually(this PlayableDirector director, double oldTime, double time) {
    var timelineAsset = director.playableAsset as TimelineAsset;
    var outputOffset = 0; // if there are no markers on markertrack timeline does not generate an output
    if (timelineAsset != null) {
      var markerCount = timelineAsset.markerTrack.GetMarkerCount();
      if (markerCount > 0) {
        var output = director.playableGraph.GetOutput(0);
        outputOffset = 1;
        for (var i = 0; i < markerCount; i++) {
          var marker = timelineAsset.markerTrack.GetMarker(i);
          director.ProcessTrackMarkers(marker, output, oldTime, time);
        }
      }
    }

    for (int i = outputOffset; i < director.playableGraph.GetOutputCount(); i++) {
      var output = director.playableGraph.GetOutput(i);
      var playable = output.GetSourcePlayable().GetInput(i);
      var track = output.GetReferenceObject() as TrackAsset;
      if (track == null)
          continue;
      var markerCount = track.GetMarkerCount();
      for (var j = 0; j < markerCount; j++) {
        var marker = track.GetMarker(j);
        director.ProcessTrackMarkers(marker, output, oldTime, time);
      }
    }
  }

  public static TaskFunc PlayTask(this PlayableDirector director, LocalTime localTime) => async scope => {
    try {
      director.RebuildGraph();
      director.timeUpdateMode = DirectorUpdateMode.Manual;
      director.extrapolationMode = DirectorWrapMode.None;
      director.time = 0;
      director.FireMarkersManually(0, 0);
      director.Evaluate();
      do {
        await scope.Tick();
        var oldTime = director.time;
        director.time += localTime.FixedDeltaTime;
        director.FireMarkersManually(oldTime, director.time);
        director.Evaluate();
      } while (director.time < director.duration);
    } catch (OperationCanceledException) {
    } catch (Exception e) {
      Debug.LogError($"Execption:{e.GetType()} {e.Message}");
    } finally {
      director.playableGraph.Destroy();
    }
  };
}