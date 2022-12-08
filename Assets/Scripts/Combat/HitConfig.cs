using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HitConfig {
  public GameObject VFX;
  public AudioClip SFX;
  public AttributeModifier DamageModifier;
  public KnockBackType KnockbackType;
  public float KnockbackStrength;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;

  static Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    return type switch {
      KnockBackType.Delta => p0.TryGetDirection(p1) ?? attacker.forward,
      KnockBackType.Forward => attacker.forward,
      KnockBackType.Back => -attacker.forward,
      KnockBackType.Right => attacker.right,
      KnockBackType.Left => -attacker.right,
      KnockBackType.Up => attacker.up,
      KnockBackType.Down => -attacker.up,
      _ => attacker.forward,
    };
  }

  static List<StatusEffect> ComputeOnHit(Attributes attacker) {
    List<StatusEffect> effects = new();
    if (attacker.GetValue(AttributeTag.BurningHits) is var dps && dps != 0)
      effects.Add(new BurningEffect(dps));
    if (attacker.GetValue(AttributeTag.FreezingHits) is var duration && duration != 0)
      effects.Add(new FreezingEffect(Timeval.FromSeconds(duration).Ticks));
    return effects;
  }

  public HitConfig Scale(HitConfig config, float scale) {
    return new() {
      VFX = VFX,
      SFX = SFX,
      DamageModifier = new AttributeModifier() {
        Base = DamageModifier.Base,
        Mult = DamageModifier.Mult*scale
      },
      KnockbackType = KnockbackType,
      KnockbackStrength = KnockbackStrength*scale,
      RecoilStrength = RecoilStrength,
      CameraShakeStrength = CameraShakeStrength,
      HitStopDuration = HitStopDuration
    };
  }

  public HitParams ComputeParams(Attributes attacker, Hurtbox hurtbox) {
    return new() {
      VFX = VFX,
      SFX = SFX,
      RecoilStrength = RecoilStrength,
      CameraShakeStrength = CameraShakeStrength,
      HitStopDuration = HitStopDuration,
      Damage = DamageModifier.Apply(attacker.GetValue(AttributeTag.Damage, 0f)),
      KnockbackStrength = attacker.GetValue(AttributeTag.Knockback, KnockbackStrength),
      KnockbackVector = KnockbackVector(attacker.transform, hurtbox.transform, KnockbackType),
      OnHitEffects = ComputeOnHit(attacker),
      WallbounceTarget = Bouncebox.ComputeWallbounceTarget(attacker.transform),
    };
  }
}