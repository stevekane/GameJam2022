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
    using (AddStatusEffect(Invulnerable)) {
      yield return Fiber.Any(Fiber.Repeat(HandleHits), ListenFor(HoldRelease));
    }
    Animator.SetBool("Shielding", false);
    yield return Recovery.Start(Animator, Index);
  }

  public IEnumerator HoldRelease() => null;

  IEnumerator HandleHits() {
    var hitEvent = Fiber.ListenFor(Status.GetComponent<Defender>().HitEvent);
    yield return hitEvent;
    (var hit, var hitTransform) = hitEvent.Value;
    if (Shield != null)
      Shield.GetComponent<Defender>().OnHit(hit, hitTransform);
  }

  public override void OnStop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    Animator.SetBool("Shielding", false);
  }
}