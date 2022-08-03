using System.Collections;
using UnityEngine;

public class LightAttackAbility : Ability {
  public Transform Owner;
  public Animator Animator;
  public AnimationClip Clip;
  public FrameEventTimeline FrameEventTimeline;

  protected override IEnumerator MakeRoutine() {
    yield return new AnimationTask(Animator, Clip) {
      FrameEventTimeline = FrameEventTimeline,
      OnFrameEvent = OnFrameEvent
    };
    Stop();
  }

  void OnFrameEvent(FrameEvent fe) {
    Debug.Log($"Something happened on frame {fe.Frame}");
  }
}