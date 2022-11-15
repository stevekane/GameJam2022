using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public Animator Animator;
  public bool Dead = false; // Set by ShieldDie animation
  Damage Damage;
  Collider Hurtbox;
  public Defender Defender { get; private set; }

  void Awake() {
    Damage = GetComponent<Damage>();
    Defender = GetComponent<Defender>();
    Hurtbox = GetComponentInChildren<Hurtbox>().GetComponent<Collider>();
  }

  void FixedUpdate() {
    if (Damage.Points > MaxDamage) {
      Animator.SetTrigger("ShieldDie");
      Hurtbox.enabled = false;
    }
    if (Dead) {
      Destroy(gameObject);
    }
  }
}
