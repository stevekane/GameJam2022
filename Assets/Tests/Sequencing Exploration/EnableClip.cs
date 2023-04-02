using UnityEngine;
using UnityEngine.Playables;

public class EnableBehavior : TaskBehavior {
  public override void Setup(Playable playable) {
    var mb = (MonoBehaviour)UserData;
    mb.enabled = true;
  }

  public override void Cleanup(Playable playable) {
    var mb = (MonoBehaviour)UserData;
    mb.enabled = false;
    Debug.Log($"{FixedFrame.Instance.Tick} {mb.GetType()} EnableClip Cleanup");
  }
}

public class EnableClip : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<EnableBehavior>.Create(graph);
  }
}