using UnityEngine;

public class HitParams {
  public HitConfig HitConfig;
  public Attributes.Serialized AttackerAttributes;
  public GameObject Attacker;
  public GameObject Source;
  public GameObject Defender;
  public Attributes.Serialized DefenderAttributes;
  public Vector3 KnockbackVector => HitConfig.KnockbackType.KnockbackVector(HitConfig.RelativeKnockbackVector, Source.transform, Defender.transform);
  public float KnockbackStrength => HitConfig.Knockback.Apply(AttackerAttributes.GetValue(AttributeTag.Knockback, 0));
  public float Damage => HitConfig.Damage.Apply(AttackerAttributes.GetValue(AttributeTag.Damage, 0));

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
  public HitParams(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject attacker, GameObject source)
    => Init(hitConfig, attackerAttributes, attacker, source);
  public HitParams(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject source)
     => Init(hitConfig, attackerAttributes, source, source);
  public HitParams(HitConfig hitConfig, Attributes attributes)
     => Init(hitConfig, attributes.serialized, attributes.gameObject, attributes.gameObject);

  void Init(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject attacker, GameObject source) {
    HitConfig = hitConfig;
    AttackerAttributes = attackerAttributes;
    Attacker = attacker;
    Source = source;
  }
}