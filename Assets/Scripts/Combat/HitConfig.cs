using System;

[Serializable]
public class HitConfig {
  public AttributeModifier DamageModifier;
  public KnockBackType KnockbackType;
  public float KnockbackStrength;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;

  public HitConfig Scale(float scale) {
    return new() {
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