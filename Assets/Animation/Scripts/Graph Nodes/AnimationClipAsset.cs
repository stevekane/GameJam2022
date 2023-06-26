using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AnimationGraph/Clip")]
public class AnimationClipAsset : PlayableAsset {
  [SerializeField] AnimationClip Clip;
  [SerializeField] bool EnableFootIK = true;
  [SerializeField] bool EnablePlayableIK = true;
  [SerializeField] float Speed = 1;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = AnimationClipPlayable.Create(graph, Clip);
    playable.SetSpeed(Speed);
    playable.SetApplyFootIK(EnableFootIK);
    playable.SetApplyPlayableIK(EnablePlayableIK);
    return playable;
  }
}