using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimatorGraphTrackBehavior : TaskBehavior {
  // TODO: Is this correct? Should the track be responsible for this?
  // Maybe clips should individually do this?
  public override void Cleanup(Playable playable) {
    var animatorGraph = (AnimatorGraph)UserData;
    animatorGraph.Disconnect();
  }
}

[TrackBindingType(typeof(AnimatorGraph))]
[TrackClipType(typeof(AnimatorGraphClip))]
public class AnimatorGraphTrack : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<AnimatorGraphTrackBehavior>.Create(graph, inputCount);
  }

  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    #if UNITY_EDITOR
    var animatorGraph = (AnimatorGraph)director.GetGenericBinding(this);
    if (!animatorGraph)
      return;
    var animator = animatorGraph.GetComponent<Animator>();
    if (!animator) {
      Debug.LogWarning($"No animator found as peer of AnimatorGraph");
      return;
    }
    if (animator.isHuman) {
      void AddTransformComponentProperties(Transform t) {
        driver.AddFromComponent(t.gameObject, t);
        var childCount = t.childCount;
        for (var i = 0; i < childCount; i++) {
          AddTransformComponentProperties(t.GetChild(i));
        }
      }
      AddTransformComponentProperties(animator.avatarRoot);
    } else {
      foreach (var timelineClip in GetClips()) {
        var clip = timelineClip.asset as AnimatorGraphClip;
        driver.AddFromClip(animator.gameObject, clip.Clip);
      }
    }
    #endif
  }
}