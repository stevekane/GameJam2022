using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AnimationGraph/Clip")]
public class AnimationClipAsset : PlayableAsset {
  [SerializeField] AnimationClip Clip;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return AnimationClipPlayable.Create(graph, Clip);
  }
}