using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class AimAt : AbilityTask {
  public Transform Aimer;
  public Transform Target;
  public float TurnSpeed;
  public AimAt(Transform aimer, Transform target, float turnSpeed) {
    Aimer = aimer;
    Target = target;
    TurnSpeed = turnSpeed;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    while (true) {
      var current = Aimer.rotation;
      var desired = Quaternion.LookRotation(Target.position.XZ()-Aimer.position.XZ(), Vector3.up);
      Aimer.rotation = Quaternion.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed);
      yield return null;
    }
  }
}

[Serializable]
public class InactiveAttackPhase : AbilityTask {
  int Index;
  Animator Animator;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public IEnumerator Start(Animator animator, int index) {
    Reset();
    Animator = animator;
    Index = index;
    return this;
  }
  public async Task Start(TaskScope scope, Animator animator, int index) {
    Reset();
    Animator = animator;
    Index = index;
    await TaskRoutine(scope);
  }
  public override IEnumerator Routine() {
    for (var i = 0; i < Duration.Ticks; i++) {
      yield return null;
      var attackSpeed = ClipDuration.Millis/Duration.Millis;
      Animator.SetFloat("AttackSpeed", attackSpeed);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
    }
  }

  public async Task TaskRoutine(TaskScope scope) {
    for (var i = 0; i < Duration.Ticks; i++) {
      await scope.Tick();
      var attackSpeed = ClipDuration.Millis/Duration.Millis;
      Animator.SetFloat("AttackSpeed", attackSpeed);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
    }
  }
}
