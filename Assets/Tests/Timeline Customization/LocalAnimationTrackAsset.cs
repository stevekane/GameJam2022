using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;

[TrackColor(.25f, .25f, .75f)]
[TrackBindingType(typeof(Animator))]
[TrackClipType(typeof(AnimationPlayableAsset), false)]
public class LocalAnimationTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return AnimationMixerPlayable.Create(graph, inputCount);
  }

  public override IEnumerable<PlayableBinding> outputs {
    get {
      yield return AnimationPlayableBinding.Create("Animation Binding", this);
    }
  }

  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var animator = director.GetGenericBinding(this) as Animator;
    if (animator == null)
      return;
    foreach (var clip in m_Clips) {
      var asset = clip.asset as AnimationPlayableAsset;
      if (asset == null)
        continue;
      driver.AddFromClip(asset.clip);
    }
  }
}