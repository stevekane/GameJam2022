using System.Collections;
using UnityEngine;

public class ShieldAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Shield Shield;

  public static InlineEffect Invulnerable {
    get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ShieldInvulnerable");
  }

  public IEnumerator HoldStart() {
    Animator.SetBool("Shielding", true);
    yield return Windup.Start(Animator, Index);
    if (Shield != null)
      Shield.Hurtbox.gameObject.SetActive(true);
    var invuln = AddStatusEffect(Invulnerable);
    Using(invuln);
    yield return ListenFor(HoldRelease);
    invuln.Dispose();
    Disposables.Remove(invuln);
    if (Shield != null)
      Shield.Hurtbox.gameObject.SetActive(false);
    Animator.SetBool("Shielding", false);
    yield return Recovery.Start(Animator, Index);
  }

  public IEnumerator HoldRelease() => null;

  public override void OnStop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Shielding", false);
    if (Shield != null)
      Shield.Hurtbox.gameObject.SetActive(false);
  }
}