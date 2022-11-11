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

  public Action<FrameEvent> OnFrameEvent;
  public EventSource<FrameEvent> FrameEventSource = new();

  public AnimationTask(Animator animator, AnimationClip clip, FrameEventTimeline timeline = null) {
    EventHead = 0;
    Animator = animator;
    Graph = PlayableGraph.Create();
    ClipPlayable = AnimationClipPlayable.Create(Graph, clip);
    ClipPlayable.SetDuration(clip.length);
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
    if (Graph.IsValid()) {
      BroadcastFrameEvents();
    }
    if (Graph.IsDone()) {
      Stop();
    }
    return IsRunning;
  }
  void BroadcastFrameEvents() {
    if (FrameEventTimeline != null && OnFrameEvent != null) {
      var clip = ClipPlayable.GetAnimationClip();
      var frames = clip.length*clip.frameRate+1;
      var time = ClipPlayable.GetTime();
      var interpolant = (float)(time/clip.length);
      var newHead = (int)Mathf.Lerp(0, frames, interpolant);
      var oldHead = EventHead;
      for (var i = oldHead; i < newHead; i++) {
        foreach (var e in FrameEventTimeline.Events) {
          if (e.Frame == i) {
            OnFrameEvent.Invoke(e);
            FrameEventSource.Fire(e);
          }
        }
      }
      EventHead = newHead;
    }
  }
}