using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(fileName = "States")]
public class AnimationStatesPlayableAsset : PlayableAsset {
  [SerializeField] List<AnimationClip> Clips;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = AnimationMixerPlayable.Create(graph);
    foreach (var clip in Clips) {
      var animationClipPlayable = AnimationClipPlayable.Create(graph, clip);
      playable.AddInput(animationClipPlayable, 0, 0);
    }
    return playable;
  }
}