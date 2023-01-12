using System;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class HomingMissile : MonoBehaviour {
  [SerializeField] float Speed = 15;
  [SerializeField] GameObject ContactVFX;
  [SerializeField] AudioClip ContactSFX;
  [SerializeField] TriggerEvent Hitbox;

  Hitter Hitter;
  Rigidbody Rigidbody;
  Transform Target;

  void Awake() {
    Rigidbody = GetComponent<Rigidbody>();
    Hitter = GetComponent<Hitter>();
    Hitbox.OnTriggerEnterSource.Listen(OnHitboxEnter);
  }

  void Start() => Target = FindObjectOfType<Player>().transform;
  void FixedUpdate() {
    if (Target) {
      Rigidbody.velocity = transform.forward * Speed;
    }
  }

  void Explode() {
    VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    SFXManager.Instance.TryPlayOneShot(ContactSFX);
    Destroy(gameObject, .01f);
  }

  void OnHitboxEnter(Collider target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      if (hurtbox.TryAttack(Hitter.HitParams))
        Explode();
    } else if (target.TryGetComponent(out Parrybox parrybox)) {
      parrybox.TryParry(Hitter.HitParams);
    }
  }

  void OnCollisionEnter(Collision c) {
    Explode();
  }

  void OnWasParried(HitParams hitParams) {
    transform.forward = hitParams.Defender.transform.forward;
    Hitter.HitParams.AttackerTeamID = hitParams.DefenderTeamID;
  }
}