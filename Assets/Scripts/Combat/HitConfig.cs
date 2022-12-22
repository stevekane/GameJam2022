using System;
using UnityEngine;

[Serializable]
public class HitConfig {
  public AttributeModifier Damage;
  public AttributeModifier Knockback;
  public KnockBackType KnockbackType;
  [Tooltip("Vector in the reference frame defined by knockbacktype, an attacker and defender")]
  public Vector3 RelativeKnockbackVector = Vector3.forward;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public Timeval StunDuration = Timeval.FromMillis(500);
  public Timeval SlowFallDuration = Timeval.FromSeconds(0);
  public bool InPlace;

  public HitConfig Scale(float scale) {
    return new() {
      Damage = new() {
        Base = Damage.Base*scale,
        Mult = Damage.Mult
      },
      Knockback = new() {
        Base = Knockback.Base*scale,
        Mult = Knockback.Mult
      },
      KnockbackType = KnockbackType,
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