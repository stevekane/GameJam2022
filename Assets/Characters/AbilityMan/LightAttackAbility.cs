using System.Collections;
using UnityEngine;

public class LightAttackAbility : Ability {
  public Transform Owner;
  public Animator Animator;
  public AnimationClip Clip;
  public FrameEventTimeline FrameEventTimeline;

  public IEnumerator AttackStart() {
    yield return new AnimationTask(Animator, Clip);
  }

  void OnFrameEvent(FrameEvent fe) {
    switch (fe.Type) {
    case FrameEventType.Begin:
      Debug.Log($"Animation Start Event on frame {fe.Frame}");
    break;
    case FrameEventType.End:
      Debug.Log($"Animation End Event on frame {fe.Frame}");
    break;
    }
  }
}