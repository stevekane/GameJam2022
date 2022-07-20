using System;
using UnityEngine;

public class Bouncebox : MonoBehaviour {
  public Timeval Duration;
  public AudioClip AudioClip;
  public GameObject Effect;
  public AudioSource AudioSource;
  Status Status;
  Collider Collider;
  Vibrator Vibrator;

  void Awake() {
    Status = GetComponentInParent<Status>();
    Vibrator = GetComponentInParent<Vibrator>();
    Collider = GetComponent<Collider>();
  }

  void FixedUpdate() {
    Collider.enabled = Status.IsAirborne;
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.tag == "Ground") return;

    var k = Status.Get<KnockbackEffect>();
    if (k != null && Physics.Raycast(transform.position, k.Velocity.normalized, out var hit)) {
      AudioSource.PlayOptionalOneShot(AudioClip);
      Vibrator.Vibrate(transform.right, Duration.Frames, .15f);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Effect, hit.point);
      var bounceVel = Vector3.Reflect(k.Velocity, hit.normal.XZ());
      Status.Remove(k);
      Status.Add(new HitStunEffect(Duration.Frames, new KnockbackEffect(bounceVel)));
    }
  }
}