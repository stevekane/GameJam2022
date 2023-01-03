using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Events;

namespace Traditional {
  [Serializable]
  public struct AnimationFrameEvent {
    public int Frame;
    public UnityEvent<AnimationPlayable> Event;
  }

  [Serializable]
  public class AnimationSpecification {
    public AnimationClip AnimationClip = null;
    public AvatarMask AvatarMask = null;
    public float Speed = 1;
    public List<AnimationFrameEvent> Events = new();
  }

  public class AnimationPlayable {
    public AnimationLayerMixerPlayable Mixer { get; }
    public AnimationClipPlayable Clip { get; }
    public PlayableGraph Graph { get; }
    public AnimationSpecification Specification { get; }
    public int CurrentFrame { get; private set; } = 0;
    internal AnimationPlayable(PlayableGraph graph, AnimationSpecification specification) {
      Graph = graph;
      Specification = specification;
      Clip = AnimationClipPlayable.Create(Graph, Specification.AnimationClip);
      Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
      Mixer.ConnectInput(1, Clip, 0, 1);
      Clip.SetSpeed(Specification.Speed);
      Clip.SetDuration(Specification.AnimationClip.length);
      Clip.SetTime(0);
      Clip.Play();
      if (specification.AvatarMask) {
        Mixer.SetLayerMaskFromAvatarMask(1, Specification.AvatarMask);
      }
    }

    internal void Tick() {
      var clip = Specification.AnimationClip;
      var frames = clip.length * clip.frameRate + 1;
      var interpolant = (float)(Clip.GetTime() / clip.length);
      var currentFrame = (int)(Mathf.Lerp(0, frames, interpolant));
      var previousFrame = CurrentFrame;
      for (var frame = previousFrame; frame < currentFrame; frame++) {
        foreach (var frameEvent in Specification.Events) {
          if (frameEvent.Frame == frame) {
            frameEvent.Event?.Invoke(this);
          }
        }
      }
      CurrentFrame = currentFrame;
    }
  }

  public class AnimationGraph : MonoBehaviour {
    [SerializeField] Animator Animator;

    PlayableGraph Graph;
    AnimatorControllerPlayable AnimatorControllerPlayable;
    AnimationPlayableOutput Output;
    AnimationPlayable CurrentPlayable;

    void Awake() {
      Graph = PlayableGraph.Create("Animation Graph");
      Output = AnimationPlayableOutput.Create(Graph, "Animation Graph", Animator);
      AnimatorControllerPlayable = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
      Output.SetSourcePlayable(AnimatorControllerPlayable);
      Graph.Play();
    }

    void OnDestroy() {
      Graph.Destroy();
    }

    void Update() {
      if (CurrentPlayable != null) {
        if (CurrentPlayable.Clip.IsNull() || !CurrentPlayable.Clip.IsValid() || CurrentPlayable.Clip.IsDone()) {
          CurrentPlayable.Clip.Destroy();
          CurrentPlayable.Mixer.Destroy();
          CurrentPlayable = null;
          Output.SetSourcePlayable(AnimatorControllerPlayable);
        } else {
          CurrentPlayable.Tick();
        }
      }
    }

    public AnimationPlayable Play(AnimationSpecification spec) {
      CurrentPlayable = new AnimationPlayable(Graph, spec);
      CurrentPlayable.Mixer.SetInputWeight(1, 1);
      CurrentPlayable.Mixer.ConnectInput(0, AnimatorControllerPlayable, 0, 1);
      Output.SetSourcePlayable(CurrentPlayable.Mixer);
      return CurrentPlayable;
    }
  }
}