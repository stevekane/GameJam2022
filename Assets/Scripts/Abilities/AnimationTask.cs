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
  AnimationPlayableOutput Output;
  FrameEventTimeline FrameEventTimeline;
  int EventHead;
  double DesiredSpeed = 1f;

  public Action<FrameEvent> OnFrameEvent;
  public EventSource<FrameEvent> FrameEventSource = new();

  public AnimationTask(Animator animator, AnimationClip clip, FrameEventTimeline timeline = null) {
    EventHead = 0;
    Animator = animator;
    ClipPlayable = AnimationClipPlayable.Create(animator.playableGraph, clip);
    ClipPlayable.SetDuration(clip.isLooping ? double.MaxValue : clip.length);
    FrameEventTimeline = timeline;
    Output = AnimationPlayableOutput.Create(animator.playableGraph, clip.name, Animator);
    Output.SetSourcePlayable(ClipPlayable);
  }
  ~AnimationTask() => Destroy();
  public void Stop() => Destroy();
  public bool IsRunning { get => ClipPlayable.IsValid() && !ClipPlayable.IsDone(); }
  public object Current { get; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (ClipPlayable.IsValid()) {
      ClipPlayable.SetSpeed(DesiredSpeed * Animator.speed);
      BroadcastFrameEvents();
      if (ClipPlayable.IsDone())
        Stop();
    }
    return IsRunning;
  }
  void Destroy() {
    if (ClipPlayable.IsValid()) {
      Animator.playableGraph.DestroySubgraph(ClipPlayable);
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
  void BroadcastFrameEvents() {
    if (FrameEventTimeline != null && OnFrameEvent != null) {
      var oldHead = EventHead;
      UpdateEventHead();
      var newHead = EventHead;
      foreach (var e in FrameEventTimeline.Events) {
        if (e.Frame >= oldHead && e.Frame < newHead) {
          OnFrameEvent.Invoke(e);
          FrameEventSource.Fire(e);
        }
      }
    }
  }
}