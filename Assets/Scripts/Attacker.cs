using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attacker : MonoBehaviour {
  public enum States { Idle, Windup, Active, Recovery }
  public States State;
  public AttackConfig LightConfig;  // TODO: one per attack
  public GameObject LightAttack;

  public bool IsAttacking { get { return State != States.Idle; } }

  int FramesRemaining = 0;

  public void Attack() {
    State = States.Windup;
    FramesRemaining = LightConfig.WindupTime.Frames;
  }

  private void Awake() {
    LightAttack.SetActive(false);
  }

  void FixedUpdate() {
    if (IsAttacking && --FramesRemaining <= 0) {
      switch (State) {
      case States.Windup:
        LightAttack.SetActive(true);
        LightAttack.GetComponent<ParticleSystem>().Play();
        State = States.Active;
        FramesRemaining = LightConfig.ActiveTime.Frames;
        break;
      case States.Active:
        State = States.Recovery;
        FramesRemaining = LightConfig.RecoveryTime.Frames;
        LightAttack.SetActive(false);
        break;
      case States.Recovery: State = States.Idle; break;
      }
    }

    if (State == States.Active) {
      var c = LightAttack.GetComponent<BoxCollider>();
    }
  }

  public void OnHit(GameObject target) {
    target.GetComponent<Mob>()?.TakeDamage();
  }
}
