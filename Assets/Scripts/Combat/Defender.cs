using System;
using UnityEngine;

[Serializable]
public class HitParams {
  public Timeval HitStopDuration;
  public float Damage;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
}

public class Defender : MonoBehaviour {
  Status Status;
  Damage Damage;

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
    if (IsBlocking || IsParrying || !Status.IsHittable)
      return;
    var power = 5f * hit.KnockbackStrength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    var hitStopFrames = hit.HitStopDuration.Frames;
    var knockBackDirection = KnockbackVector(hitTransform, transform, hit.KnockbackType);
    Status?.Add(new HitStopEffect(knockBackDirection, .15f, hitStopFrames), (s) => s.Add(new KnockbackEffect(knockBackDirection*power)));
    if (Status.IsDamageable) {
      Damage?.AddPoints(hit.Damage);
    }
  }

  public void Die() {
    // TODO: keep track of last attacker
    SendMessage("OnDeath");
    Destroy(gameObject, .01f);
  }

  void Awake() {
    // Note: GetComponentInParent is probably wrong. Badger's shield has a Defender so it can be destructible, but it's missing
    // a bunch of other components like Status and Animator.
    Status = GetComponentInParent<Status>();
    Damage = GetComponent<Damage>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    if (transform.position.y < -100f) {
      Die();
    }
  }
}