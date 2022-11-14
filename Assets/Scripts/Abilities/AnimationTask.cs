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
  PlayableGraph Graph;
  FrameEventTimeline FrameEventTimeline;
  int EventHead;
  double DesiredSpeed = 1f;

  public Action<FrameEvent> OnFrameEvent;
  public EventSource<FrameEvent> FrameEventSource = new();

  public AnimationTask(Animator animator, AnimationClip clip, FrameEventTimeline timeline = null) {
    EventHead = 0;
    Animator = animator;
    Graph = PlayableGraph.Create();
    ClipPlayable = AnimationClipPlayable.Create(Graph, clip);
    ClipPlayable.SetDuration(clip.isLooping ? double.MaxValue : clip.length);
    FrameEventTimeline = timeline;
    Output = AnimationPlayableOutput.Create(Graph, clip.name, Animator);
    Output.SetSourcePlayable(ClipPlayable);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    Graph.Play();
  }
  ~AnimationTask() => Graph.Destroy();
  public void Stop() => Graph.Destroy();
  public bool IsRunning { get => Graph.IsValid() && !Graph.IsDone(); }
  public object Current { get; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    ClipPlayable.SetSpeed(DesiredSpeed * Animator.speed);
    if (Graph.IsValid()) {
      BroadcastFrameEvents();
    }
    if (Graph.IsDone()) {
      Stop();
    }
    return IsRunning;
  }
  public void ResetAnimation() {
    if (Graph.IsValid()) {
      Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
      ClipPlayable.SetTime(0);
      Graph.Play();
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