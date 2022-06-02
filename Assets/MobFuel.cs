using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobFuel : Mob {
  Animator Animator;

  void Start() {
    Animator = GetComponent<Animator>();
  }

  public override void TakeDamage() {
    // TODO: need instant explode too
    Animator.SetTrigger("PreExplode");
  }

  public void Explode() {
    Destroy(gameObject);
    // spawn explosion
    // damage nearby
  }
}
