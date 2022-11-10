using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HitConfig {
  public AttributeModifier DamageModifier;
  public Timeval HitStopDuration;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
  public AudioClip SFX;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public HitParams ComputeParams(Attributes attacker) {
    return new() {
      Damage = DamageModifier.Apply(attacker.GetValue(AttributeTag.Damage, 0f)),
      KnockbackStrength = attacker.GetValue(AttributeTag.Knockback, KnockbackStrength),
      KnockbackType = KnockbackType,
      HitStopDuration = HitStopDuration,
      SFX = SFX,
      VFX = VFX,
      VFXOffset = VFXOffset,
      OnHitEffects = ComputeOnHit(attacker),
    };
  }
  List<StatusEffect> ComputeOnHit(Attributes attacker) {
    List<StatusEffect> effects = new();
    if (attacker.GetValue(AttributeTag.BurningHits) is var dps && dps != 0)
      effects.Add(new BurningEffect(dps));
    if (attacker.GetValue(AttributeTag.FreezingHits) is var duration && duration != 0)
      effects.Add(new FreezingEffect(Timeval.FromSeconds(duration).Frames));
    return effects;
  }
}

[Flags]
public enum HitFlags {
  Freezing = 1<<0,
  Burning = 1<<1,
}

public class HitParams {
  public float Damage;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
  public Timeval HitStopDuration;
  public AudioClip SFX;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public List<StatusEffect> OnHitEffects;
}

public class Defender : MonoBehaviour {
  Optional<Status> Status;
  Damage Damage;
  bool PlayingFallSound;
  public Vector3? LastGroundedPosition { get; private set; }

  public AudioClip FallSFX;
  public bool IsParrying;
  public bool IsBlocking;

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
    SFXManager.Instance.TryPlayOneShot(hit.SFX);
    VFXManager.Instance.TrySpawnEffect(hit.VFX, transform.position + hit.VFXOffset);

    if (IsBlocking || IsParrying || !(Status?.Value.IsHittable ?? true))
      return;
    var power = 5f * hit.KnockbackStrength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    var knockBackDirection = KnockbackVector(hitTransform, transform, hit.KnockbackType);
    Status?.Value.Add(new HitStopEffect(knockBackDirection, .15f, hit.HitStopDuration.Frames),
      s => s.Add(new KnockbackEffect(knockBackDirection*power)));
    Damage.AddPoints(hit.Damage);
    hit.OnHitEffects.ForEach(e => Status?.Value.Add(e));
  }

  public void Die() {
    // TODO: keep track of last attacker
    LastGroundedPosition = LastGroundedPosition ?? transform.position;
    SendMessage("OnDeath", SendMessageOptions.RequireReceiver);
  }

  void Awake() {
    // Note: GetComponentInParent is probably wrong. Badger's shield has a Defender so it can be destructible, but it's missing
    // a bunch of other components like Status and Animator.
    Status = GetComponentInParent<Status>();
    Damage = GetComponent<Damage>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    if (transform.position.y < 0f && !PlayingFallSound) {
      LastGroundedPosition = transform.position;
      PlayingFallSound = true;
      SFXManager.Instance.TryPlayOneShot(FallSFX);
    }
    if (transform.position.y < -100f) {
      Die();
    }
  }
}