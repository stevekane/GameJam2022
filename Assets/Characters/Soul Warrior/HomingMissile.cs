using System;
using UnityEngine;

public class HomingMissile : MonoBehaviour {
  [SerializeField] float Speed = 15;
  [SerializeField] GameObject ContactVFX;
  [SerializeField] AudioClip ContactSFX;

  [NonSerialized] public HitParams HitParams;

  Rigidbody Rigidbody;
  Transform Target;

  void Awake() => Rigidbody = GetComponent<Rigidbody>();
  void Start() => Target = FindObjectOfType<Player>().transform;
  void FixedUpdate() {
    if (Target) {
      var delta = Target.position-transform.position;
      var toTarget = delta.normalized;
      var rotation = Quaternion.LookRotation(toTarget);
      Rigidbody.velocity = transform.forward * Speed;
    }
  }

  void OnTriggerEnter(Collider target) {
    VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    SFXManager.Instance.TryPlayOneShot(ContactSFX);
    Destroy(gameObject);
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.TryAttack(HitParams);
    }
  }
}