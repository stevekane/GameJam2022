using System;
using System.Collections.Generic;
using UnityEngine.Timeline;

public static class TimelineExtensions {
  public static IEnumerable<TrackAsset> Tracks(
  this TimelineAsset timelineAsset,
  Predicate<Type> predicate) {
    foreach (var track in timelineAsset.GetOutputTracks()) {
      foreach (var output in track.outputs) {
        if (predicate(output.outputTargetType)) {
          yield return track;
        }
      }
    }
  }
}