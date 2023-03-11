using UnityEngine;
using UnityEngine.Playables;

public abstract class TaskBehavior : PlayableBehaviour {
  bool Active;
  bool JustActive;
  protected object UserData;
  public virtual void Setup(Playable playable) {}
  public virtual void Cleanup(Playable playable) {}

  public override void OnBehaviourPause(Playable playable, FrameData info) {
    if (Active)
      Cleanup(playable);
    Active = false;
    JustActive = false;
  }

  public override void OnBehaviourPlay(Playable playable, FrameData info) {
    Active = true;
    JustActive = true;
  }

  public override void OnPlayableDestroy(Playable playable) {
    if (Active)
      Cleanup(playable);
  }

  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    UserData = playerData;
    if (JustActive)
      Setup(playable);
    JustActive = false;
  }
}

public class TaskClipTest : TaskBehavior {
  public override void Setup(Playable playable) {
    Debug.Log("Task clip setup");
  }

  public override void Cleanup(Playable playable) {
    Debug.Log("Task clip cleanup");
  }
}


public class TaskClip : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<TaskClipTest>.Create(graph);
  }
}