using UnityEngine;
using UnityEngine.Playables;

public class SpawnClipBehavior : TaskBehavior {
  public GameObject Prefab;
  public override void Setup(Playable playable) {
    var referenceObject = (GameObject)UserData;
    GameObject.Instantiate(Prefab, referenceObject.transform.position, referenceObject.transform.rotation);
  }
}

public class SpawnClip : PlayableAsset {
  public GameObject Prefab;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<SpawnClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Prefab = Prefab;
    return playable;
  }
}