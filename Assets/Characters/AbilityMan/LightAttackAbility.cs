using System.Collections;
using UnityEngine;

public class LightAttackAbility : Ability {
  public Transform Owner;
  public Animator Animator;
  public AnimationClip Clip;

  protected override IEnumerator MakeRoutine() {
    yield return new AnimationTask(Animator, Clip, OnEvent);
    Stop();
  }

  void OnEvent(int frame) {
    Debug.Log($"Something happened on frame {frame}");
  }
}