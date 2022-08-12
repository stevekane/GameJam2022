using System.Collections;
using UnityEngine;

public class ShieldAbility : Ability {
  public int Index;
  public Animator Animator;
  public EventTag ReleaseEvent;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public bool IsRaised;

  public IEnumerator HoldStart() {
    Animator.SetBool("Shielding", true);
    yield return Windup.Start(Animator, Index);
    IsRaised = true;
    yield return Fiber.ListenFor(GetComponentInParent<AbilityManager>().GetEvent(HoldRelease));
    IsRaised = false;
    Animator.SetBool("Shielding", false);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public IEnumerator HoldRelease() => null;

  public override void Stop() {
    IsRaised = false;
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Shielding", false);
    base.Stop();
  }
}