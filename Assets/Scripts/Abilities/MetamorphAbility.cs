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
  public float DamageFactor = 2f;
  public float KnockbackFactor = 1.5f;

  public IEnumerator Begin() {
    AddStatusEffect(new InlineEffect((status) => {
    // Animator.SetBool(metamorph);
      status.Tags.ClearFlags(AbilityTag.BaseForm);
      status.Tags.AddFlags(AbilityTag.MorphForm);
      status.DamageFactor *= DamageFactor;
      status.KnockbackFactor *= KnockbackFactor;
    }));
    yield return Fiber.Any(Fiber.Wait(Duration.Frames), Fiber.ListenFor(AbilityManager.GetEvent(End)));
    Stop();
  }

  public IEnumerator End() => null;

  public override void Stop() {
    //Animator.SetBool(metamorph);
    base.Stop();
  }
}
