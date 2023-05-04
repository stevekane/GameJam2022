using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

public static class PlayableDirectorExtensions {
  public static async Task PlayTask(this PlayableDirector director, TaskScope scope, LocalTime localTime) {
    try {
      director.RebuildGraph();
      director.timeUpdateMode = DirectorUpdateMode.Manual;
      director.extrapolationMode = DirectorWrapMode.None;
      director.time = 0;
      director.Evaluate();
      do {
        await scope.Tick();
        director.time += localTime.FixedDeltaTime;
        director.Evaluate();
      } while (director.time < director.duration);
    } catch (OperationCanceledException) {
    } catch (Exception e) {
      Debug.LogError(e.Message);
    } finally {
      director.playableGraph.Destroy();
    }
  }
}