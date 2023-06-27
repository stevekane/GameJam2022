using System;
using UnityEngine;

namespace Archero {
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
      //var defenderWeightFactor = 2f / (1f + DefenderAttributes.GetValue(AttributeTag.Weight));
      var defenderWeightFactor = 1f / DefenderAttributes.GetValue(AttributeTag.Weight);
      var baseKnockback = (defenderDamage/10f + (defenderDamage * Damage)/20f) * defenderWeightFactor * 1.4f + 18f;
      // TODO HACK: This is a different attribute formula.. maybe this is what we want in general?
      return HitConfig.Knockback.Base + HitConfig.Knockback.Mult * baseKnockback;
    }

    // Scale hitstop and hurtstun duration based on damage.
    static AnimationCurve StunScaling = AnimationCurve.Linear(0, 1f, 1, 3f); // 1-3x duration
    float ScaleDuration(float baseDuration, float defenderDamage) =>
      baseDuration * StunScaling.Evaluate(defenderDamage / 100f);
    public Timeval GetHitStopDuration(float defenderDamage) =>
      Timeval.FromSeconds(ScaleDuration(HitConfig.HitStopDuration.Seconds, defenderDamage));
    public Timeval GetHurtStunDuration(float defenderDamage) =>
      Timeval.FromSeconds(ScaleDuration(HitConfig.HitStopDuration.Seconds, defenderDamage));

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
}