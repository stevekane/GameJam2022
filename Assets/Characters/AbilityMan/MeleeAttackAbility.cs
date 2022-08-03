using System.Collections;
using UnityEngine;

public class MeleeAttackAbility : AbilityFibered {
  public AnimationClip Clip;
  public Transform Owner;
  public Animator Animator;

  public override void Activate() {
    var duration = Clip.length;
    var frameRate = Clip.frameRate;
    var frames = duration*frameRate;
    base.Activate();
  }

  protected override IEnumerator MakeRoutine() {
    yield return new AnimationTask(Animator, Clip, OnHit);
    Stop();
  }

  void OnHit(int frame) {
    Debug.Log($"Something happened on frame {frame}");
  }
}