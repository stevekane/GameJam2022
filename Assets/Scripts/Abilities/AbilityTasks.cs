using System;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class InactiveAttackPhase {
  int Index;
  Animator Animator;
  public Timeval Duration = Timeval.FromMillis(0);
  public Timeval ClipDuration = Timeval.FromMillis(0);
  public async Task Start(TaskScope scope, Animator animator, int index) {
    Animator = animator;
    Index = index;
    await TaskRoutine(scope);
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
