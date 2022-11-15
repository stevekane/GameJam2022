using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public static class AnimatorTastExtensions {
  public static AnimationTask Run(this Animator animator, AnimationClip clip) {
    return new AnimationTask(animator, clip);
  }
}

public class AnimationTask : IEnumerator, IStoppable {
  Animator Animator;
  AnimationClipPlayable ClipPlayable;
  AnimationLayerMixerPlayable Mixer;
  AnimationPlayableOutput Output;
  int EventHead;
  double DesiredSpeed = 1f;

  public AnimationTask(Animator animator, AnimationClip clip, bool additive = false) {
    EventHead = 0;
    Animator = animator;

    ClipPlayable = AnimationClipPlayable.Create(animator.playableGraph, clip);
    ClipPlayable.SetDuration(clip.isLooping ? double.MaxValue : clip.length);

    Mixer = AnimationLayerMixerPlayable.Create(Animator.playableGraph, 1);
    Mixer.SetLayerAdditive(0, additive);
    Mixer.SetInputWeight(0, 1f);

    Animator.playableGraph.Connect(ClipPlayable, 0, Mixer, 0);

    Output = AnimationPlayableOutput.Create(animator.playableGraph, clip.name, Animator);
    Output.SetSourcePlayable(Mixer);
  }
  ~AnimationTask() => Destroy();
  public void Stop() => Destroy();
  public bool IsRunning { get => ClipPlayable.IsValid() && !ClipPlayable.IsDone(); }
  public object Current { get; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (ClipPlayable.IsValid()) {
      ClipPlayable.SetSpeed(DesiredSpeed * Animator.speed);
      if (ClipPlayable.IsDone())
        Stop();
    }
    return IsRunning;
  }
  void Destroy() {
    if (ClipPlayable.IsValid()) {
      Animator.playableGraph.DestroySubgraph(Mixer); // destroys inputs too
      Animator.playableGraph.DestroyOutput(Output);
    }
  }
  public void SetSpeed(double speed) => DesiredSpeed = speed;
  public IEnumerator PlayUntil(int frame) {
    while (EventHead < frame && MoveNext()) {
      UpdateEventHead();
      yield return null;
    }
  }
  void UpdateEventHead() {
    var clip = ClipPlayable.GetAnimationClip();
    var frames = (clip.length*clip.frameRate+1)/* / Animator.speed*/;
    var time = ClipPlayable.GetTime();
    var interpolant = (float)(time/clip.length);
    EventHead = (int)Mathf.Lerp(0, frames, interpolant);
  }
}