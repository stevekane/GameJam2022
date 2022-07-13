using UnityEngine;

enum Motion { Base, Dashing }

public class Vapor : MonoBehaviour {
  public static Quaternion RotationFromInputs(Transform t, float speed, Action action, float dt) {
    var desiredForward = 
      action.Aim.XZ.TryGetDirection() ??
      action.Move.XZ.TryGetDirection() ?? 
      t.forward; 
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = dt*speed;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  public static Vector3 HeadingFromInputs(Transform t, Action action) {
    var heading = 
      action.Move.XZ.TryGetDirection() ??
      action.Aim.XZ.TryGetDirection() ??
      t.forward;
    return heading;
  }

  public static Vector3 VelocityFromMove(Action action, float speed) {
    return speed*action.Move.XZ;
  }

  [SerializeField] float MOVE_SPEED;
  [SerializeField] float TURN_SPEED;
  [SerializeField] float DASH_SPEED;
  [SerializeField] Timeval DashDuration;
  [SerializeField] Attacker Attacker;
  [SerializeField] Cannon Cannon;
  [SerializeField] Pushable Pushable;
  [SerializeField] CharacterController Controller;

  Motion Motion;
  Vector3 Velocity;
  Vector3 DashHeading;
  float DashFramesRemaining;

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    
    if (action.Jump.JustDown) {
      Cannon.DepressTrigger();
    } else if (action.Jump.Down) {
      Cannon.HoldTrigger();
    } else if (action.Jump.JustUp) {
      Cannon.ReleaseTrigger();
    }

    if (action.Hit.JustDown && Motion == Motion.Base) {
      DashHeading = HeadingFromInputs(transform, action);
      DashFramesRemaining = DashDuration.Frames;
      Motion = Motion.Dashing;
    } else if (Motion == Motion.Dashing && DashFramesRemaining <= 0) {
      Motion = Motion.Base;
    }

    if (Motion == Motion.Base) {
      Velocity = VelocityFromMove(action, MOVE_SPEED)+Pushable.Impulse;
      Controller.Move(dt*Velocity);
    } else if (Motion == Motion.Dashing) {
      DashFramesRemaining--;
      Controller.Move(dt*DASH_SPEED*DashHeading);
    }
    transform.rotation = RotationFromInputs(transform, TURN_SPEED, action, dt);
  }
}