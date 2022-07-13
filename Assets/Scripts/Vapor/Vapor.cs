using UnityEngine;

enum Motion { Base, Dashing }

public class Vapor : MonoBehaviour {
  public static Quaternion RotationFromInputs(Transform t, float speed, Action action, float dt) {
    var desiredForward = action.Right.XZ.TryGetDirection() ?? t.forward;
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = dt*speed;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  public static Vector3 HeadingFromInputs(Transform t, Action action) {
    var heading = 
      action.Left.XZ.TryGetDirection() ??
      action.Right.XZ.TryGetDirection() ??
      t.forward;
    return heading;
  }

  public static Vector3 VelocityFromMove(Action action, float speed) {
    return speed*action.Left.XZ;
  }

  [SerializeField] float MOVE_SPEED;
  [SerializeField] float DASH_SPEED;
  [SerializeField] float TURN_SPEED;
  [SerializeField] float FIRING_TURN_SPEED;
  [SerializeField] Timeval DashDuration;
  [SerializeField] Attacker Attacker;
  [SerializeField] Cannon Cannon;
  [SerializeField] Pushable Pushable;
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;

  Motion Motion;
  Vector3 Velocity;
  Vector3 DashHeading;
  float DashFramesRemaining;
  int PunchCycleIndex;

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    
    if (action.L2.JustDown) {
      Cannon.DepressTrigger();
    } else if (action.L2.Down) {
      Cannon.HoldTrigger();
    } else if (action.L2.JustUp) {
      Cannon.ReleaseTrigger();
    }

    if (action.R2.JustDown && Motion == Motion.Base) {
      DashHeading = HeadingFromInputs(transform, action);
      DashFramesRemaining = DashDuration.Frames;
      Motion = Motion.Dashing;
    } else if (Motion == Motion.Dashing && DashFramesRemaining <= 0) {
      Motion = Motion.Base;
    }

    if (action.R1.JustDown && !Attacker.IsAttacking) {
      PunchCycleIndex = PunchCycleIndex <= 0 ? 1 : 0;
      Attacker.StartAttack(0);
    }

    Attacker.Step(dt);
    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetInteger("AttackSequenceIndex", PunchCycleIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);

    if (Motion == Motion.Base) {
      Velocity = VelocityFromMove(action, MOVE_SPEED)+Pushable.Impulse;
      Controller.Move(dt*Velocity);
    } else if (Motion == Motion.Dashing) {
      DashFramesRemaining--;
      Controller.Move(dt*DASH_SPEED*DashHeading);
    }
    
    var turnSpeed = Cannon.IsFiring ? FIRING_TURN_SPEED : TURN_SPEED;
    transform.rotation = RotationFromInputs(transform, turnSpeed, action, dt);
  }
}