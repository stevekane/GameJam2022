using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobShitter : Mob {
  public Bullet BulletPrefab;
  public MobShitterConfig Config;

  enum MobShitterState { Idle, Shoot }
  Animator Animator;

  MobShitterState State {
    get { return (MobShitterState)Animator.GetInteger("State"); }
    set { Animator.SetInteger("State", (int)value); }
  }

  void Start() {
    Animator = GetComponent<Animator>();
    State = MobShitterState.Idle;
  }

  void Update() {
    switch (State) {
    case MobShitterState.Idle:
      var player = GameObject.FindObjectOfType<Player>();
      var playerDelta = (player.transform.position - transform.position);
      var playerInRange = playerDelta.sqrMagnitude < Config.ShootRadius*Config.ShootRadius;
      if (playerInRange) {
        State = MobShitterState.Shoot;
      }
      break;
    }
  }

  public void Shoot() {
    var player = GameObject.FindObjectOfType<Player>();
    var playerDir = (player.transform.position - transform.position).normalized;
    Bullet.Fire(BulletPrefab, transform.position + Vector3.up*.5f + playerDir, playerDir, Config.BulletSpeed);
  }

  public void ShootCooldown() {
    State = MobShitterState.Idle;
  }

  public override void TakeDamage() {
    Animator.SetTrigger("Die");
  }

  public void Die() {
    Destroy(gameObject);
  }


  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Config.ShootRadius);
  }
}
