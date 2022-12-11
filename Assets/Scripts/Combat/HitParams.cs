using UnityEngine;

public class HitParams {
  public HitConfig HitConfig;
  public Attributes.Serialized AttackerAttributes;
  public GameObject Attacker;
  public GameObject Source;
  public GameObject Defender;
  public Attributes.Serialized DefenderAttributes;
  public Vector3 KnockbackVector => HitConfig.KnockbackType.KnockbackVector(Source.transform, Defender.transform);
  public float KnockbackStrength => AttackerAttributes.GetValue(AttributeTag.Knockback, HitConfig.KnockbackStrength);
  public float Damage => HitConfig.DamageModifier.Apply(AttackerAttributes.GetValue(AttributeTag.Damage, 0));

  // Useful if HitParams are reused for multiple calls to Hurtbox.TryAttack(), which will reset the Defender.
  public HitParams Clone() => new() {
    HitConfig = HitConfig,
    AttackerAttributes = AttackerAttributes,
    Attacker = Attacker,
    Source = Source,
    Defender = Defender,
    DefenderAttributes = DefenderAttributes,
  };

  HitParams() { }
  public HitParams(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject attacker, GameObject source) {
    HitConfig = hitConfig;
    AttackerAttributes = attackerAttributes;
    Attacker = attacker;
    Source = source;
  }

  public HitParams(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject source) {
    HitConfig = hitConfig;
    AttackerAttributes = attackerAttributes;
    Attacker = source;
    Source = source;
  }

  public HitParams(HitConfig hitConfig, Attributes attributes) {
    HitConfig = hitConfig;
    AttackerAttributes = attributes.serialized;
    Attacker = attributes.gameObject;
    Source = attributes.gameObject;
  }
}