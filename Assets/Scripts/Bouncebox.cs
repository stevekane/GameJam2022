using System;
using System.Linq;
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
      var dot = Vector3.Dot(k.Velocity, hit.Value.normal);
      if (Damage.Points > 100f) {
        Status.Remove(k);
        Instantiate(Explosion, transform.position, Quaternion.identity);
        if (Damage.TryGetComponent(out Defender d))
          d.Die();
      } else {
        SFXManager.Instance.TryPlayOneShot(AudioClip);
        VFXManager.Instance.TrySpawnEffect(Effect, hit.Value.point);
        var bounceVel = Vector3.Reflect(k.Velocity, hit.Value.normal.XZ());
        Status.Remove(k);
        Status.Add(new HitStopEffect(transform.right, .15f, Duration.Ticks), (s) => s.Add(new KnockbackEffect(bounceVel)));
      }
    }
  }

  // Finds the hit point and normal for the triggered collision. Has logic to ignore nearby walls that we didn't bump into at a sufficient angle.
  RaycastHit? FindCollisionPoint(Collider collider, Vector3 dir) {
    var numHits = Physics.RaycastNonAlloc(transform.position, dir, PhysicsBuffers.RaycastHits, MaxDistance);
    var idx = Array.FindIndex(PhysicsBuffers.RaycastHits[..numHits], h => h.collider == collider && Vector3.Dot(h.normal, dir) < 0f);
    return idx >= 0 ? PhysicsBuffers.RaycastHits[idx] : null;
  }
}