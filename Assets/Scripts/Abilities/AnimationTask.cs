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
  double Duration;
  int EventHead;

  public FrameEventTimeline FrameEventTimeline;
  public Action<FrameEvent> OnFrameEvent;

  public AnimationTask(Animator animator, AnimationClip clip) {
    IsRunning = true;
    EventHead = 0;
    Animator = animator;
    Graph = PlayableGraph.Create();
    Duration = clip.isLooping ? double.MaxValue : clip.length;
    ClipPlayable = AnimationClipPlayable.Create(Graph, clip);
    ClipPlayable.SetDuration(Duration);
    Output = AnimationPlayableOutput.Create(Graph, "AnimationTask", Animator);
    Output.SetSourcePlayable(ClipPlayable);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    Graph.Play();
  }
  ~AnimationTask() {
    ClipPlayable.Destroy();
    Graph.Destroy();
  }
  public void Stop() {
    ClipPlayable.Destroy();
    Graph.Destroy();
    IsRunning = false;
  }
  public bool IsRunning { get; set; }
  public object Current { get; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (!Graph.IsDone()) {
      if (FrameEventTimeline != null && OnFrameEvent != null) {
        var clip = ClipPlayable.GetAnimationClip();
        var frames = clip.length*clip.frameRate+1;
        var time = ClipPlayable.GetTime();
        var interpolant = (float)(time/Duration);
        var newHead = (int)Mathf.Lerp(0, frames, interpolant);
        var oldHead = EventHead;
        for (var i = oldHead; i < newHead; i++) {
          foreach (var e in FrameEventTimeline.Events) {
            if (e.Frame == i) {
              OnFrameEvent.Invoke(e);
            }
          }
        }
        EventHead = newHead;
      }
      return true;
    } else {
      if (IsRunning) {
        Stop();
      }
      return false;
    }
  }
}