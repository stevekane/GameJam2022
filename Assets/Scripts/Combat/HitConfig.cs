using System;
using UnityEngine;

[Serializable]
public class HitConfig {
  [Tooltip("Final damage = Base + Mult*CharacterDamageAttrib")]
  public AttributeModifier Damage;
  [Tooltip("Final knockback = Base + Mult*CharacterKnockbackAttrib")]
  public AttributeModifier Knockback;
  public KnockbackType KnockbackType;
  [Tooltip("Angle (degrees) above the horizontal plane along the direction defined by knockbacktype, an attacker and defender")]
  public float KnockbackAngle = 0;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public Timeval StunDuration = Timeval.FromMillis(500);
  public Timeval SlowFallDuration = Timeval.FromSeconds(0);

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
      KnockbackAngle = KnockbackAngle,
      RecoilStrength = RecoilStrength,
      CameraShakeStrength = CameraShakeStrength,
      HitStopDuration = HitStopDuration,
      StunDuration = new Timeval() { Ticks = (int)(StunDuration.Ticks*scale) },
      SlowFallDuration = SlowFallDuration,
    };
  }
}