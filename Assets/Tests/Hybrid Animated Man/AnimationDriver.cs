using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public class PlayableAnimation {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
}

[RequireComponent(typeof(Animator))]
public class AnimationDriver : MonoBehaviour {
  Animator Animator;
  PlayableGraph Graph;
  AnimatorControllerPlayable AnimatorController;
  AnimationPlayableOutput Output;

  // Unity initializes private member variables to default(T) which SOMEHOW is not null?!
  [NonSerialized] Fiber Fiber = null;
  [NonSerialized] AnimationLayerMixerPlayable? Mixer = null;
  [NonSerialized] AnimationClipPlayable? Clip = null;

  void Awake() {
    Fiber = null;
    Mixer = null;
    Clip = null;
    Animator = GetComponent<Animator>();
    Graph = PlayableGraph.Create("Animation Driver");
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Output = AnimationPlayableOutput.Create(Graph, "Animation Driver", Animator);
    Output.SetSourcePlayable(AnimatorController);
  }

  void OnDestroy() {
    Stop();
    Graph.Destroy();
  }

  void FixedUpdate() {
    if (Fiber != null) {
      var stillRunning = Fiber.MoveNext();
      if (!stillRunning) {
        Stop();
      }
    }
  }

  public void SetSpeed(float speed) {
    Clip?.SetSpeed(speed);
  }

  public IEnumerator Play(PlayableAnimation animation) {
    Stop();
    Fiber = new Fiber(InnerPlay(animation));
    return Fiber.While(() => Fiber != null && Fiber.IsRunning);
  }

  void Stop() {
    Fiber?.Stop();
    Fiber = null;
    Mixer?.SetInputWeight(1, 0);
    Mixer?.DisconnectInput(1);
    Mixer?.Destroy();
    Mixer = null;
    Clip?.Destroy();
    Clip = null;
    Output.SetSourcePlayable(AnimatorController);
    Graph.Stop();
  }

  IEnumerator InnerPlay(PlayableAnimation animation) {
    Clip = AnimationClipPlayable.Create(Graph, animation.Clip);
    Clip.Value.SetSpeed(animation.Speed);
    Clip.Value.SetDuration(animation.Clip.length);
    Mixer = AnimationLayerMixerPlayable.Create(Graph, 2);
    Mixer.Value.ConnectInput(0, AnimatorController, 0, 1);
    Mixer.Value.ConnectInput(1, Clip.Value, 0, 1);
    Mixer.Value.SetLayerMaskFromAvatarMask(1, animation.Mask);
    Output.SetSourcePlayable(Mixer.Value);
    Graph.Play();
    yield return Clip.Value.UntilDone();
  }
}