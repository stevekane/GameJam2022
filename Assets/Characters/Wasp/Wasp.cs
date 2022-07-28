using UnityEngine;

public class Wasp : MonoBehaviour {
  public float ShootRadius = 20f;
  public float MoveSpeed = 15f;
  public Timeval ShootDelay = Timeval.FromMillis(1000);
  CharacterController Controller;
  Status Status;
  Animator Animator;
  Transform Target;
  int FramesRemaining = 0;
  Vector3 Velocity;
  SimpleAbility CurrentAbility;
  SimpleAbility[] Abilities;

  enum StateType { Idle, Chase, Shoot, Kite }
  StateType State = StateType.Idle;

  bool TryStartAbility(SimpleAbility ability) {
    if (ability.IsComplete) {
      ability.Begin();
      CurrentAbility = ability;
      return true;
    } else {
      return false;
    }
  }

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Abilities = GetComponentsInChildren<SimpleAbility>();
  }

  void FixedUpdate() {
    if (Target == null)
      return;

    if (CurrentAbility?.IsComplete ?? false)
      CurrentAbility = null;

    Velocity.SetXZ(Vector3.zero);
    var gravity = -200f * Time.fixedDeltaTime;
    Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;

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
        if (targetInRange && Status.CanAttack && CurrentAbility == null) {
          State = StateType.Shoot;
          TryStartAbility(Abilities[0]);
        } else if (Status.CanMove) {
          Velocity.SetXZ(dir * MoveSpeed);
        }
        break;
      }
    case StateType.Shoot: {
        var targetDelta = (Target.transform.position - transform.position);
        var dir = targetDelta.normalized;
        transform.forward = dir;
        if (CurrentAbility == null) {
          State = StateType.Kite;
        }
        break;
      }
    case StateType.Kite: {
        var targetDelta = (Target.transform.position - transform.position);
        var desiredDist = ShootRadius - 5f;
        if (targetDelta.sqrMagnitude < desiredDist*desiredDist && Status.CanMove) {
          var dir = -targetDelta.normalized;
          Velocity.SetXZ(dir * MoveSpeed);
          transform.forward = dir;
        } else {
          State = StateType.Idle;
          FramesRemaining = ShootDelay.Frames;
        }
        break;
      }
    }

    Controller.Move(Velocity*Time.fixedDeltaTime);

    Animator.SetBool("HitFlinch", Status.IsHitstun);
  }
}
