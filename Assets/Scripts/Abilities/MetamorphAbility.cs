using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InlineEffect : StatusEffect {
  Action<Status> ApplyFunc;
  public InlineEffect(Action<Status> apply) => ApplyFunc = apply;
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) => ApplyFunc(status);
}

public class MetamorphAbility : Ability {
  public Timeval Duration = Timeval.FromSeconds(10f);
  public AttributeModifier DamageModifier = new() { Mult = 2 };
  public AttributeModifier KnockbackModifier = new() { Mult = 1.5f };
  Animator Animator;

  public IEnumerator Begin() {
    Animator.SetBool("Morph", true);
    AddStatusEffect(new InlineEffect((status) => {
      status.Tags.ClearFlags(AbilityTag.AbilityHeavyEnabled);
      status.Tags.ClearFlags(AbilityTag.AbilityMorphEnabled);
      status.Tags.AddFlags(AbilityTag.AbilitySlamEnabled);
      status.Tags.AddFlags(AbilityTag.AbilitySuplexEnabled);
      status.AddAttributeModifier(AttributeTag.Damage, DamageModifier);
      status.AddAttributeModifier(AttributeTag.Knockback, KnockbackModifier);
    }));
    yield return Fiber.Any(Fiber.Wait(Duration.Frames), Fiber.ListenFor(AbilityManager.GetEvent(End)));
  }

  public IEnumerator End() => null;

  public override void OnStop() {
    Animator.SetBool("Morph", false);
  }

  void Awake() {
    Animator = GetComponentInParent<Animator>();
  }
}
