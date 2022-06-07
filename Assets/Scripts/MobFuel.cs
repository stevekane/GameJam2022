using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobFuel : Mob {
  public GameObject ExplosionPrefab;
  Animator Animator;

  void Start() {
    Animator = GetComponent<Animator>();
  }

  public override void TakeDamage() {
    Explode();
    // TODO: Do we still need the blinky-explode sequence?
    //Destroy(GetComponent<MobMove>());
    //Animator.SetTrigger("PreExplode");
  }

  public void Explode() {
    Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
    Destroy(gameObject, .1f);
  }
}
