using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Traditional {
    [Serializable]
  public struct AnimationFrameEvent {
    public int Frame;
    public UnityEvent<AnimationClipPlayable> Event;
  }

  [Serializable]
  public class AnimationSpecification {
    public AnimationClip AnimationClip = null;
    public AvatarMask AvatarMask = null;
    public float Speed = 1;
    public int StartFrame = 0;
    public int EndFrame = 0;
    public List<AnimationFrameEvent> Events = new();
  }

  [Serializable]
  public class AnimationMontageSectionSpecification {
    public string Name;
    public AnimationSpecification AnimationSpecification;
  }

  [Serializable]
  public class AnimationMontageSpecification {
    public List<AnimationMontageSectionSpecification> Sections;
  }

  public class AnimationMontagePlayable : PlayableBehaviour {
    AnimationMontageSpecification Specification;
    AnimationLayerMixerPlayable Mixer;
    public void Initialize(PlayableGraph graph, AnimationMontageSpecification specification) {
      Specification = specification;
      Mixer = AnimationLayerMixerPlayable.Create(graph, 2);
      Mixer.SetInputWeight(0, 0);
      Mixer.SetInputWeight(1, 0);
    }

    public void Play(string name) {
      var section = Specification.Sections.Find(s => s.Name == name);
      if (section != null) {
        Debug.Log($"Found section with name {name} to play");
      }
    }
  }

  public class AnimationTimelinePlayable : PlayableBehaviour {
    AnimationClipPlayable AnimationClipPlayable;
    AnimationSpecification Specification;
    int PlayHead;

    public static AnimationTimelinePlayable Create(PlayableGraph graph, AnimationSpecification specification) {
      var playable = ScriptPlayable<AnimationTimelinePlayable>.Create(graph, 1);
      var behavior = playable.GetBehaviour();
      return playable.GetBehaviour();
    }

    public override void OnPlayableCreate(Playable playable) {
      Debug.Log("Playable Created");
    }

    public override void OnPlayableDestroy(Playable playable) {
      // destroy the associated AnimationClipPlayable
      Debug.Log("Playable destroyed");
    }

    public override void PrepareFrame(Playable playable, FrameData info) {
      playable.SetTime(playable.GetTime() + info.deltaTime);
    }
  }

  public class AnimationGraph : MonoBehaviour {
    [SerializeField] Animator Animator;

    PlayableGraph Graph;
    AnimationLayerMixerPlayable Mixer;
    AnimatorControllerPlayable AnimatorControllerPlayable;
    AnimationPlayableOutput Output;
    List<AnimationSpecification> Specifications = new();
    List<AnimationClipPlayable> Playables = new();
    List<int> PlayHeads = new();

    void Awake() {
      Graph = Animator.playableGraph;
      AnimatorControllerPlayable = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
      Mixer = AnimationLayerMixerPlayable.Create(Graph, 1);
      Mixer.ConnectInput(0, AnimatorControllerPlayable, 0, 1);
      Output = AnimationPlayableOutput.Create(Graph, "Animation Graph", Animator);
      Output.SetSourcePlayable(Mixer);
    }

    void OnDestroy() {
      Graph.Destroy();
    }

    void Update() {
      for (var i = 0; i < Playables.Count; i++) {
        var playable = Playables[i];
        var specification = Specifications[i];
        var playhead = PlayHeads[i];
        if (!playable.IsNull() && playable.IsValid()) {
          UpdatePlayHead(i);
          if (playable.IsDone() || playhead >= specification.EndFrame) {
            Stop(playable);
          }
        }
      }
    }

    void UpdatePlayHead(int index) {
      var specification = Specifications[index];
      var playable = Playables[index];
      var previousPlayHead = PlayHeads[index];
      var clip = playable.GetAnimationClip();
      var frames = clip.length * clip.frameRate;
      var interpolant = (float)(playable.GetTime() / clip.length);
      var nextPlayHead = (int)(Mathf.Lerp(0, frames, interpolant));
      for (var frame = previousPlayHead+1; frame <= nextPlayHead; frame++) {
        TryFireFrameEvent(specification.Events, frame, playable);
      }
      PlayHeads[index] = nextPlayHead;
    }

    void TryFireFrameEvent(List<AnimationFrameEvent> events, int frame, AnimationClipPlayable playable) {
      foreach (var frameEvent in events) {
        if (frameEvent.Frame == frame) {
          frameEvent.Event?.Invoke(playable);
        }
      }
    }

    public AnimationClipPlayable Play(AnimationSpecification spec) {
      var clip = AnimationClipPlayable.Create(Graph, spec.AnimationClip);
      var mixerIndex = Mixer.AddInput(clip, 0, 1);
      Mixer.SetLayerMaskFromAvatarMask((uint)mixerIndex, spec.AvatarMask ? spec.AvatarMask : new AvatarMask());
      Specifications.Add(spec);
      Playables.Add(clip);
      PlayHeads.Add(spec.StartFrame);
      clip.SetSpeed(spec.Speed);
      clip.SetDuration(spec.AnimationClip.length);
      clip.SetTime(spec.StartFrame);
      if (spec.EndFrame > spec.StartFrame) {
        TryFireFrameEvent(spec.Events, spec.StartFrame, clip);
        clip.Play();
      } else {
        Stop(clip);
      }
      return clip;
    }

    public void Stop(AnimationClipPlayable playable) {
      var index = Playables.IndexOf(playable);
      if (index >= 0) {
        playable.Destroy();
        Specifications.RemoveAt(index);
        Playables.RemoveAt(index);
        PlayHeads.RemoveAt(index);
        Mixer.SetInputCount(Mixer.GetInputCount()-1);
        for (var i = 0; i < Playables.Count; i++) {
          var spec = Specifications[i];
          var port = i+1; // AnimationController always port0
          Mixer.ConnectInput(port, Playables[i], 0, 1);
          Mixer.SetLayerMaskFromAvatarMask((uint)port, spec.AvatarMask ? spec.AvatarMask : new AvatarMask());
        }
      } else {
        Debug.LogError($"Tried to stop {playable.GetAnimationClip().name} not owned by {name}");
      }
    }
  }
}