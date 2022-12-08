using System;
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

  public HitConfig Scale(float scale) {
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
}