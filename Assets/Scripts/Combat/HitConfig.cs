using System;
using UnityEngine;

[Serializable]
public class HitConfig {
  public AttributeModifier DamageModifier;
  public KnockBackType KnockbackType;
  [Tooltip("Vector in the reference frame defined by knockbacktype, an attacker and defender")]
  public Vector3 RelativeKnockbackVector = Vector3.forward;
  public float KnockbackStrength;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public Timeval StunDuration = Timeval.FromMillis(500);
  public Timeval SlowFallDuration = Timeval.FromSeconds(0);
  public bool InPlace;

  public HitConfig Scale(float scale) {
    return new() {
      DamageModifier = new AttributeModifier() {
        Base = DamageModifier.Base,
        Mult = DamageModifier.Mult*scale
      },
      KnockbackType = KnockbackType,
      KnockbackStrength = KnockbackStrength*scale,
      RelativeKnockbackVector = RelativeKnockbackVector,
      RecoilStrength = RecoilStrength,
      CameraShakeStrength = CameraShakeStrength,
      HitStopDuration = HitStopDuration,
      StunDuration = new Timeval() { Ticks = (int)(StunDuration.Ticks*scale) },
      SlowFallDuration = SlowFallDuration,
      InPlace = InPlace
    };
  }
}