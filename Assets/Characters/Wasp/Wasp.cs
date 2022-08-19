using System.Linq;
using UnityEngine;

public class Wasp : MonoBehaviour {
  public float ShootRadius = 20f;
  public float MoveSpeed = 15f;
  public Timeval ShootDelay = Timeval.FromMillis(1000);
  Status Status;
  Transform Target;
  AbilityManager Abilities;
  int FramesRemaining = 0;
  Ability CurrentAbility;
  PelletAbility Pellet;

  enum StateType { Idle, Chase, Shoot, Kite }
  StateType State = StateType.Idle;

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    Status = GetComponent<Status>();
    Abilities = GetComponent<AbilityManager>();
    Pellet = GetComponentInChildren<PelletAbility>();
  }

  void FixedUpdate() {
    if (Target == null)
      return;

    if (!CurrentAbility?.IsRunning ?? false)
      CurrentAbility = null;
    var desiredMoveDir = Vector3.zero;
    var desiredFacing = transform.forward;

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
        desiredFacing = dir;
        if (targetInRange && Status.CanAttack && CurrentAbility == null) {
          State = StateType.Shoot;
          Abilities.GetEvent(Pellet.AttackStart).Fire();
          CurrentAbility = Abilities.Abilities.FirstOrDefault((a) => a.IsRunning);
        } else {
          desiredMoveDir = dir;
        }
        break;
      }
    case StateType.Shoot: {
        var targetDelta = (Target.transform.position - transform.position);
        var dir = targetDelta.normalized;
        desiredFacing = dir;
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
          desiredMoveDir = dir;
          desiredFacing = dir;
        } else {
          State = StateType.Idle;
          FramesRemaining = ShootDelay.Frames;
        }
        break;
      }
    }

    Abilities.GetAxis(AxisTag.Move).Update(0f, new Vector2(desiredMoveDir.x, desiredMoveDir.z));
    Abilities.GetAxis(AxisTag.Aim).Update(0f, new Vector2(desiredFacing.x, desiredFacing.z));
  }
}
