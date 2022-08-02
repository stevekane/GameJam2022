using System.Collections;
using UnityEngine;

public class ShieldAbility : AbilityFibered {
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

  protected override IEnumerator MakeRoutine() {
    IsHolding = true;
    Animator.SetBool("Shielding", true);
    yield return Windup.Start(Animator, Index);
    IsRaised = true;
    while (IsHolding)
      yield return null;
    IsRaised = false;
    Animator.SetBool("Shielding", false);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public override void Stop() {
    IsRaised = false;
    IsHolding = false;
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Shielding", false);
    base.Stop();
  }
}