using System;
using UnityEngine;

public class Bouncebox : MonoBehaviour {
  public Timeval Duration;
  public AudioClip AudioClip;
  public GameObject Effect;
  public GameObject Explosion;
  public AudioSource AudioSource;
  Damage Damage;
  Status Status;
  Collider Collider;
  Vibrator Vibrator;

  void Awake() {
    Damage = GetComponentInParent<Damage>();
    Status = GetComponentInParent<Status>();
    Vibrator = GetComponentInParent<Vibrator>();
    Collider = GetComponent<Collider>();
  }

  void FixedUpdate() {
    Collider.enabled = Status.Get<KnockbackEffect>()?.IsAirborne ?? false;
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = Status.Get<KnockbackEffect>();
    if (k != null && Physics.Raycast(transform.position, k.Velocity.normalized, out var hit)) {
      if (Damage.Points > 100f) {
        Status.Remove(k);
        Instantiate(Explosion, transform.position, Quaternion.identity);
        Destroy(Damage.gameObject, .01f);
      } else {
        AudioSource.PlayOptionalOneShot(AudioClip);
        VFXManager.Instance?.TrySpawnEffect(Effect, hit.point);
        var bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
        Status.Remove(k);
        Status.Add(new HitStopEffect(transform.right, .15f, Duration.Frames), (s) => s.Add(new KnockbackEffect(bounceVel)));
      }
    }
  }
}