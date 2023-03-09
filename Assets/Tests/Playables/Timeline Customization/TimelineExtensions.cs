using System;
using System.Collections.Generic;
using UnityEngine;
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

  public static int TickDuration(
  this TimelineAsset timelineAsset,
  int ticksPerSecond) {
    var seconds = timelineAsset.durationMode == TimelineAsset.DurationMode.BasedOnClips
      ? timelineAsset.duration
      : timelineAsset.fixedDuration;
    return Mathf.RoundToInt((float)seconds * ticksPerSecond);
  }
}