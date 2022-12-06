using System;
using System.Collections.Generic;
using UnityEngine;

// Configuration for HitParams. Use this to set values on an attack in the editor.
[Serializable]
public class HitConfig {
  public AttributeModifier DamageModifier;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public AudioClip SFX;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public HitParams ComputeParamsDontUse() {
    Debug.LogWarning($"Don't serialize HitParams directly. Use HitConfig, and call ComputeParams() with an attacker. Projectiles should be passed the computed params on spawn in case they outlive their attacker.");
    return ComputeParams(DamageModifier.Apply(0f), KnockbackStrength, new(), null);
  }
  public HitParams ComputeParams(Attributes attacker) => ComputeParams(
    DamageModifier.Apply(attacker.GetValue(AttributeTag.Damage, 0f)),
    attacker.GetValue(AttributeTag.Knockback, KnockbackStrength),
    ComputeOnHit(attacker),
    Bouncebox.ComputeWallbounceTarget(attacker.transform)
    );
  // A variant for scaled damage and knockback for use with charged abilities.
  public HitParams ComputeParamsScaled(Attributes attacker, float scale) => ComputeParams(
    DamageModifier.Apply(attacker.GetValue(AttributeTag.Damage, 0f)) * scale,
    attacker.GetValue(AttributeTag.Knockback, KnockbackStrength) * scale,
    ComputeOnHit(attacker),
    Bouncebox.ComputeWallbounceTarget(attacker.transform)
    );

  HitParams ComputeParams(float damage, float knockbackStrength, List<StatusEffect> onHitEffects, Vector3? wallbounceTarget) {
    return new() {
      Damage = damage,
      KnockbackStrength = knockbackStrength,
      KnockbackType = KnockbackType,
      HitStopDuration = HitStopDuration,
      SFX = SFX,
      VFX = VFX,
      VFXOffset = VFXOffset,
      OnHitEffects = onHitEffects,
      WallbounceTarget = wallbounceTarget,
    };
  }
  List<StatusEffect> ComputeOnHit(Attributes attacker) {
    List<StatusEffect> effects = new();
    if (attacker.GetValue(AttributeTag.BurningHits) is var dps && dps != 0)
      effects.Add(new BurningEffect(dps));
    if (attacker.GetValue(AttributeTag.FreezingHits) is var duration && duration != 0)
      effects.Add(new FreezingEffect(Timeval.FromSeconds(duration).Ticks));
    return effects;
  }
}

// Per-hit data. Only create this via HitConfig.ComputeParams at the time the attack is initiated. Projectiles, etc
// will want to store this until detonation.
public class HitParams {
  public float Damage;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
  public float RecoilStrength;
  public float CameraShakeStrength;
  public Timeval HitStopDuration;
  public AudioClip SFX;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public List<StatusEffect> OnHitEffects;
  public Vector3? WallbounceTarget;
}

public class Defender : MonoBehaviour {
  Optional<Status> Status;
  Damage Damage;
  bool PlayingFallSound;
  bool Died = false;
  public Vector3? LastGroundedPosition { get; private set; }

  public EventSource<(HitParams, Transform)> HitEvent = new();
  public AudioClip FallSFX;

  public static Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
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

  public void OnHit(HitParams hit, Transform hitTransform) {
    HitEvent.Fire((hit, hitTransform));
    if (!(Status?.Value.IsHittable ?? true))
      return;

    var power = 5f * hit.KnockbackStrength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    var knockBackDirection = KnockbackVector(hitTransform, transform, hit.KnockbackType);
    var rotation = Quaternion.LookRotation(knockBackDirection);

    SFXManager.Instance.TryPlayOneShot(hit.SFX);
    VFXManager.Instance.TrySpawnEffect(hit.VFX, transform.position + hit.VFXOffset, rotation);

    Status?.Value.Add(new HitStopEffect(knockBackDirection, .15f, hit.HitStopDuration.Ticks),
      s => s.Add(new KnockbackEffect(knockBackDirection*power, hit.WallbounceTarget)));
    Damage.AddPoints(hit.Damage);
    hit.OnHitEffects?.ForEach(e => Status?.Value.Add(e));
    gameObject.SendMessage("OnDamage", hit, SendMessageOptions.DontRequireReceiver);
  }

  public void Die() {
    if (Died)
      return;
    Died = true;
    // TODO: keep track of last attacker
    LastGroundedPosition = LastGroundedPosition ?? transform.position;
    SendMessage("OnDeath", SendMessageOptions.RequireReceiver);
  }

  void Awake() {
    // Note: GetComponentInParent is probably wrong. Badger's shield has a Defender so it can be destructible, but it's missing
    // a bunch of other components like Status and Animator.
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    if (transform.position.y < -1f && !PlayingFallSound) {
      LastGroundedPosition = transform.position;
      PlayingFallSound = true;
      SFXManager.Instance.TryPlayOneShot(FallSFX);
    }
    if (transform.position.y < -100f) {
      Die();
    }
  }
}