using System;
using UnityEngine;

public class Bouncebox : MonoBehaviour {
  public Timeval Duration;
  public AudioClip AudioClip;
  public GameObject Effect;
  public GameObject Explosion;
  Damage Damage;
  Status Status;
  Collider Collider;
  readonly float MaxDistance = 2.0f; // heuristic to avoid misjudging the collision point

  void Awake() {
    Damage = GetComponentInParent<Damage>();
    Status = GetComponentInParent<Status>();
    Collider = GetComponent<Collider>();
  }

  void FixedUpdate() {
    Collider.enabled = Status.Get<KnockbackEffect>()?.IsAirborne ?? false;
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = Status.Get<KnockbackEffect>();
    if (k != null && FindCollisionPoint(other, k.Velocity.normalized) is var hit && hit != null) {
      if (Damage.Points > 100f && false) {
        Status.Remove(k);
        Instantiate(Explosion, transform.position, Quaternion.identity);
        if (Damage.TryGetComponent(out Defender d))
          d.Die();
      } else {
        SFXManager.Instance.TryPlayOneShot(AudioClip);
        VFXManager.Instance.TrySpawnEffect(Effect, hit.Value.point);
        Vector3 bounceVel;
        if (k.WallbounceTarget.HasValue) {
          var bounceDelta = k.WallbounceTarget.Value - hit.Value.point;
          bounceVel = KnockbackEffect.GetSpeedToTravelDistance(bounceDelta.magnitude) * bounceDelta.normalized;
        } else {
          bounceVel = Vector3.Reflect(k.Velocity, hit.Value.normal.XZ());
        }
        Status.Remove(k);
        // Note we drop WallbounceTarget for the new knockback. If it bounces again, we use normal reflection to avoid getting stuck.
        Status.Add(new HitStopEffect(transform.right, .15f, Duration.Ticks), (s) => s.Add(new KnockbackEffect(bounceVel, null)));
      }
    }
  }

  // Finds the hit point and normal for the triggered collision. Has logic to ignore nearby walls that we didn't bump into at a sufficient angle.
  RaycastHit? FindCollisionPoint(Collider collider, Vector3 dir) {
    var numHits = Physics.RaycastNonAlloc(transform.position, dir, PhysicsQuery.RaycastHits, MaxDistance);
    var idx = Array.FindIndex(PhysicsQuery.RaycastHits[..numHits], h => h.collider == collider && Vector3.Dot(h.normal, dir) < 0f);
    return idx >= 0 ? PhysicsQuery.RaycastHits[idx] : null;
  }

  // Find a point near the attacker to bounce his victim towards (pre-computed and referenced when wallbounce occurs).
  public static Vector3 ComputeWallbounceTarget(Transform attacker) {
    var randAngle = UnityEngine.Random.Range(0, 360f);
    var dir = Quaternion.AngleAxis(randAngle, Vector3.up) * attacker.forward;
    var distance = 6f;
    var targetPos = attacker.position + distance*dir;
    if (Physics.Raycast(attacker.position, dir, distance, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
      targetPos = attacker.position;  // There's a wall in the way, just use the attacker's position.
    return targetPos;
  }
}