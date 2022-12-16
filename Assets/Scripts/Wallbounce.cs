using UnityEngine;

public class Wallbounce : MonoBehaviour {
  public Timeval Duration;
  public AudioClip AudioClip;
  public GameObject Effect;
  public GameObject Explosion;
  float ExplodeAtDamage = float.PositiveInfinity;
  Damage Damage;
  Status Status;

  void Awake() {
    Damage = GetComponentInParent<Damage>();
    Status = GetComponentInParent<Status>();
  }

  void OnControllerColliderHit(ControllerColliderHit hit) {
    if ((Defaults.Instance.EnvironmentLayerMask & (1<<hit.gameObject.layer)) == 0) return;
    if (hit.gameObject.tag == "Ground") return;
    if (Status.Get<KnockbackEffect>() is var k && k != null) {
      if (Damage.Points > ExplodeAtDamage) {
        Status.Remove(k);
        Instantiate(Explosion, transform.position, Quaternion.identity);
        if (Damage.TryGetComponent(out Defender d))
          d.Die();
      } else {
        SFXManager.Instance.TryPlayOneShot(AudioClip);
        VFXManager.Instance.TrySpawnEffect(Effect, hit.point);
        Vector3 bounceVel;
        if (k.WallbounceTarget.HasValue) {
          var bounceDelta = k.WallbounceTarget.Value - hit.point;
          bounceVel = KnockbackEffect.GetSpeedToTravelDistance(bounceDelta.magnitude) * bounceDelta.normalized;
        } else {
          bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
        }
        Status.Remove(k);
        // Note we drop WallbounceTarget for the new knockback. If it bounces again, we use normal reflection to avoid getting stuck.
        Status.Add(new HitStopEffect(transform.right, Duration.Ticks), s => s.Add(new KnockbackEffect(bounceVel, null)));
      }
    }
  }

  // Find a point near the attacker to bounce his victim towards (pre-computed and referenced when wallbounce occurs).
  // Uses the current tick as a random seed for the main angle so that multi-hits bounce to the same destination.
  public static Vector3 ComputeWallbounceTarget(Transform attacker) {
    var randAngle = (float)new System.Random(Timeval.TickCount).NextDouble() * 360f + UnityEngine.Random.Range(-15f, 15f);
    var dir = Quaternion.AngleAxis(randAngle, Vector3.up) * attacker.forward;
    var distance = 6f;
    var targetPos = attacker.position + distance*dir;
    if (Physics.Raycast(attacker.position, dir, distance, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
      targetPos = attacker.position;  // There's a wall in the way, just use the attacker's position.
    return targetPos;
  }
}