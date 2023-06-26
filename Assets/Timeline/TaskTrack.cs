using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class TaskTrackTest : TaskBehavior {
  public override void Setup(Playable playable) {
    Debug.Log("Task track setup");
  }

  public override void Cleanup(Playable playable) {
    Debug.Log("Task track cleanup");
  }
}

[TrackBindingType(typeof(GameObject))]
[TrackClipType(typeof(TaskClip))]
public class TaskTrack : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<TaskTrackTest>.Create(graph, inputCount);
  }
}