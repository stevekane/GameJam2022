using System.Threading.Tasks;
using UnityEngine;

public class MetamorphAbility : Ability {
  public Timeval Duration = Timeval.FromSeconds(10f);
  public AttributeModifier DamageModifier = new() { Mult = 2 };
  public AttributeModifier KnockbackModifier = new() { Mult = 1.5f };
  Animator Animator;

  public override async Task MainAction(TaskScope scope) {
    try {
      Animator.SetBool("Morph", true);
      using var e = Status.Add(new InlineEffect((status) => {
        status.Tags.ClearFlags(AbilityTag.AbilityHeavyEnabled);
        status.Tags.ClearFlags(AbilityTag.AbilityMorphEnabled);
        status.Tags.AddFlags(AbilityTag.AbilitySlamEnabled);
        status.Tags.AddFlags(AbilityTag.AbilitySuplexEnabled);
        status.AddAttributeModifier(AttributeTag.Damage, DamageModifier);
        status.AddAttributeModifier(AttributeTag.Knockback, KnockbackModifier);
      }));
      await scope.Any(Waiter.Delay(Duration), ListenFor(MainRelease));
    } finally {
      Animator.SetBool("Morph", false);
    }
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
  }
}
