using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationTask : IEnumerator {
  Animator Animator;
  AnimationClipPlayable ClipPlayable;
  AnimationPlayableOutput Output;
  PlayableGraph Graph;
  double Duration;
  int EventHead;
  System.Action<int> OnEvent;

  /*
  TODO:
  Implement new base class for AbilityTask that does not assume
  you will be wrapping an IEnumerator. It should instead extend
  IEnumerator but add explicit Activate/Stop methods ala AbilityFibered
  which will be called in the event a Task is stopped forcibly for
  whatever reason.
  */

  public AnimationTask(Animator animator, AnimationClip clip, System.Action<int> onEvent) {
    EventHead = 0;
    Animator = animator;
    OnEvent = onEvent;
    Graph = PlayableGraph.Create();
    Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    ClipPlayable = AnimationClipPlayable.Create(Graph, clip);
    Duration = clip.isLooping ? double.MaxValue : clip.length;
    ClipPlayable.SetDuration(Duration);
    Output = AnimationPlayableOutput.Create(Graph, "AnimationTask", Animator);
    Output.SetSourcePlayable(ClipPlayable);
    Graph.Play();
  }
  ~AnimationTask() {
    Animator = null;
    ClipPlayable.Destroy();
    Graph.Destroy();
  }

  public object Current { get; }
  public bool MoveNext() {
    var clip = ClipPlayable.GetAnimationClip();
    var frames = clip.length*clip.frameRate+1;
    var time = ClipPlayable.GetTime();
    var interpolant = (float)(time/Duration);
    var newHead = (int)Mathf.Lerp(0, frames, interpolant);
    var oldHead = EventHead;
    for (var i = oldHead; i < newHead; i++) {
      OnEvent?.Invoke(i);
    }
    EventHead = newHead;
    if (Graph.IsDone()) {
      Graph.Destroy();
      ClipPlayable.Destroy();
      return false;
    } else {
      return true;
    }
  }
  public void Reset() {
    throw new NotImplementedException("Reset not implemented for AnimationTask!");
  }
}