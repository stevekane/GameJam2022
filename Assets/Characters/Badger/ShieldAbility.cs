using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ShieldAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Shield Shield;

  public static InlineEffect Invulnerable => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "ShieldInvulnerable");

  public async Task HoldStart(TaskScope scope) {
    try {
      Animator.SetBool("Shielding", true);
      await Windup.Start(scope, Animator, Index);
      if (Shield && Shield.Hurtbox)
        Shield.Hurtbox.gameObject.SetActive(true);
      using (Status.Add(Invulnerable)) {
        await scope.ListenFor(AbilityManager.GetEvent(HoldRelease));
      }
      if (Shield && Shield.Hurtbox)
        Shield.Hurtbox.gameObject.SetActive(false);
      Animator.SetBool("Shielding", false);
      await Recovery.Start(scope, Animator, Index);
    } finally {
      Animator.SetBool("Attacking", false);
      Animator.SetInteger("AttackIndex", -1);
      Animator.SetFloat("AttackSpeed", 1);
      Animator.SetBool("Shielding", false);
      if (Shield && Shield.Hurtbox)
        Shield.Hurtbox.gameObject.SetActive(false);
    }
  }
  public Task HoldRelease(TaskScope _) => null;
}