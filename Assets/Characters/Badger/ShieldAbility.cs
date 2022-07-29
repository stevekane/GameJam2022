using System.Collections;
using UnityEngine;

public class ShieldAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public bool IsRaised;
  bool IsHolding;

  public void Release() {
    IsHolding = false;
  }

  public override IEnumerator Routine() {
    IsHolding = true;
    Animator.SetBool("Shielding", true);
    yield return Windup.Start(Animator, Index);
    IsRaised = true;
    yield return new WaitUntil(() => !IsHolding);
    IsRaised = false;
    Animator.SetBool("Shielding", false);
    yield return Recovery.Start(Animator, Index);
  }

  public override void AfterEnd() {
    IsRaised = false;
    IsHolding = false;
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Shielding", false);
  }
}