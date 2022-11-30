using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public Animator Animator;
  public AnimationClip DeathClip;
  Damage Damage;
  Collider Hurtbox;
  Bundle Bundle = new();
  public Defender Defender { get; private set; }

  IEnumerator Die() {
    yield return new AnimationTask(Animator, DeathClip, true);
    Destroy(gameObject, .01f);
  }

  void Awake() {
    Damage = GetComponent<Damage>();
    Defender = GetComponent<Defender>();
    Hurtbox = GetComponentInChildren<Hurtbox>().GetComponent<Collider>();
  }

  void FixedUpdate() {
    if (Damage.Points > MaxDamage && Hurtbox.enabled) {
      Hurtbox.enabled = false;
      Bundle.StartRoutine(Die());
    }
    Bundle.MoveNext();
  }
}
