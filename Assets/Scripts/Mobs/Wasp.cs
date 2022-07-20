using UnityEngine;

public class Wasp : MonoBehaviour {
  public float ShootRadius = 20f;
  public float MoveSpeed = 15f;
  public Timeval ShootDelay = Timeval.FromMillis(1000);
  CharacterController Controller;
  Status Status;
  Animator Animator;
  Attacker Attacker;
  Transform Target;
  int FramesRemaining = 0;

  enum StateType { Idle, Chase, Shoot, Kite }
  StateType State = StateType.Idle;

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Attacker = GetComponentInChildren<Attacker>();
  }

  void FixedUpdate() {
    switch (State) {
    case StateType.Idle:
      if (--FramesRemaining <= 0) {
        State = StateType.Chase;
        break;
      }
      break;
    case StateType.Chase: {
        var targetDelta = (Target.transform.position - transform.position);
        var targetInRange = targetDelta.sqrMagnitude < ShootRadius*ShootRadius;
        var dir = targetDelta.normalized;
        transform.forward = dir;
        if (targetInRange && Status.CanAttack) {
          State = StateType.Shoot;
          Attacker.StartAttack(0);
        } else if (Status.CanMove) {
          Controller.Move(dir * MoveSpeed * Time.fixedDeltaTime);
        }
        break;
      }
    case StateType.Shoot: {
        var targetDelta = (Target.transform.position - transform.position);
        var dir = targetDelta.normalized;
        transform.forward = dir;
        if (!Attacker.IsAttacking) {
          State = StateType.Kite;
        }
        break;
      }
    case StateType.Kite: {
        var targetDelta = (Target.transform.position - transform.position);
        var desiredDist = ShootRadius - 5f;
        if (targetDelta.sqrMagnitude < desiredDist*desiredDist && Status.CanMove) {
          var dir = -targetDelta.normalized;
          Controller.Move(dir * MoveSpeed * Time.fixedDeltaTime);
          transform.forward = dir;
        } else {
          State = StateType.Idle;
          FramesRemaining = ShootDelay.Frames;
        }
        break;
      }
    }

    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);
    Animator.SetBool("HitFlinch", Status.IsHitstun);
  }
}
