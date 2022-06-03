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
    // TODO: need instant explode too
    Destroy(GetComponent<MobMove>());
    Animator.SetTrigger("PreExplode");
  }

  public void Explode() {
    Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);
    //var particles = explosion.GetComponent<ParticleSystem>();
    //particles.Play();
    Destroy(gameObject);
    // damage nearby
  }
}
