using System;
using UnityEngine;

public class HomingMissile : MonoBehaviour {
  [SerializeField] float Speed = 15;
  [SerializeField] GameObject ContactVFX;
  [SerializeField] AudioClip ContactSFX;

  Hitter Hitter;
  Rigidbody Rigidbody;
  Transform Target;

  void Awake() {
    Rigidbody = GetComponent<Rigidbody>();
    Hitter = GetComponent<Hitter>();
  }
  void Start() => Target = FindObjectOfType<Player>().transform;
  void FixedUpdate() {
    if (Target) {
      Rigidbody.velocity = transform.forward * Speed;
    }
  }

  void OnTriggerEnter(Collider target) {
    VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    SFXManager.Instance.TryPlayOneShot(ContactSFX);
    Destroy(gameObject);
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.TryAttack(Hitter.HitParams);
    }
  }
}