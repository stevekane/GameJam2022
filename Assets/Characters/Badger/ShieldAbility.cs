using System.Collections;
using UnityEngine;

public class ShieldAbility : SimpleAbility {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public bool IsActive;
  bool IsHeld;

  public void Release() {
    IsHeld = false;
  }

  public override IEnumerator Routine() {
    IsHeld = true;
    Animator.SetBool("Held", true);
    yield return Windup.Start(Animator, Index);
    IsActive = true;
    yield return new WaitUntil(() => IsHeld == false);
    Animator.SetBool("Held", false);
    IsActive = false;
    yield return Recovery.Start(Animator, Index);
  }

  public override void AfterEnd() {
    IsActive = false;
    IsHeld = false;
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Held", false);
  }
}