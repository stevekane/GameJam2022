using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public Animator Animator;
  Damage Damage;
  Collider Hurtbox;

  void Awake() {
    Damage = GetComponent<Damage>();
    Hurtbox = GetComponentInChildren<Hurtbox>().GetComponent<Collider>();
  }

  void FixedUpdate() {
    if (Damage.Points > MaxDamage && Hurtbox.enabled) {
      Animator.SetTrigger("Shield Die");
      Hurtbox.enabled = false;
    }
    if (!gameObject.activeSelf) {
      Destroy(gameObject);
    }
  }
}
