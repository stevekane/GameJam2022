using UnityEngine;

public class HitParams {
  public HitConfig HitConfig;
  public GameObject Attacker;  // Might be dead by the time hit is processed.
  public GameObject Source;    // Source of damage (e.g. the attacker for melee, fireball for projectile, etc)
  public GameObject Defender;  // Initialized by Hurtbox when processing a hit.
  public int AttackerTeamID;
  public Attributes.Serialized AttackerAttributes;
  public IAttributes DefenderAttributes;
  // TODO: cache this value? It gets called at least 4x per hit.
  public Vector3 KnockbackVector => HitConfig.KnockbackType.KnockbackVector(HitConfig.KnockbackAngle, Source.transform, Defender.transform);
  public float Damage => HitConfig.Damage.Apply(AttackerAttributes.GetValue(AttributeTag.Damage, 0));
  public int DefenderTeamID => Defender.GetComponent<Team>().ID;

  public float GetKnockbackStrength(float defenderDamage) {
    var defenderWeightFactor = 2f / (1f + DefenderAttributes.GetValue(AttributeTag.Weight));
    return HitConfig.Knockback.Apply((defenderDamage/10f + (defenderDamage * Damage)/20f) * defenderWeightFactor * 1.4f + 18f);
  }

  // Useful if HitParams are reused for multiple calls to Hurtbox.TryAttack(), which will reset the Defender.
  public HitParams Clone() => new() {
    HitConfig = HitConfig,
    Attacker = Attacker,
    Source = Source,
    Defender = Defender,
    AttackerTeamID = AttackerTeamID,
    AttackerAttributes = AttackerAttributes,
    DefenderAttributes = DefenderAttributes,
  };

  HitParams() { }
  public HitParams(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject attacker, GameObject source)
    => Init(hitConfig, attackerAttributes, attacker, source);
  public HitParams(HitConfig hitConfig, Attributes attributes)
     => Init(hitConfig, attributes.SerializedCopy, attributes.gameObject, attributes.gameObject);

  void Init(HitConfig hitConfig, Attributes.Serialized attackerAttributes, GameObject attacker, GameObject source) {
    HitConfig = hitConfig;
    AttackerAttributes = attackerAttributes;
    Attacker = attacker;
    Source = source;
    AttackerTeamID = attacker.GetComponent<Team>().ID;
  }
}