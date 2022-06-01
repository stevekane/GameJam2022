using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobShitter : Mob {
  public enum MobShitterState { Idle, PreShoot }
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
    var player = GameObject.FindObjectOfType<Player>();
    var playerDelta = (player.transform.position - transform.position);
    var playerInRange = playerDelta.sqrMagnitude < 100; // TODO

    switch (State) {
    case MobShitterState.Idle:
      if (playerInRange) {
        Debug.Log("Player in range, gonna shoot");
        State = MobShitterState.PreShoot;
      }
      break;
    case MobShitterState.PreShoot:
      if (!playerInRange) {
        Debug.Log("Player left range");
        State = MobShitterState.Idle;
      }
      break;
    }
  }

  public void Shoot() {
    Debug.Log("PEW PEW");
    State = MobShitterState.Idle;
  }

  public override void TakeDamage() {
    StartCoroutine(DestroySequence());
  }

  IEnumerator DestroySequence() {
    const float duration = .5f;
    float timer = 0f;
    Vector3 scale = transform.localScale;
    while (timer < duration) {
      transform.localScale = scale * (1 - timer/duration);
      yield return null;
      timer += Time.deltaTime;
    }
    Destroy(gameObject);
  }
}
