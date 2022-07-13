using UnityEngine;

enum Motion { Base, Dashing }

public class Vapor : MonoBehaviour {
  public static float MOVE_SPEED = 15f;
  public static float TURN_SPEED = 720f;

  [SerializeField] Attacker Attacker;
  [SerializeField] Cannon Cannon;
  [SerializeField] Pushable Pushable;
  [SerializeField] CharacterController Controller;

  Motion Motion;

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

  public static Vector3 VelocityFromMove(Action action, float speed) {
    return speed*action.Move.XZ;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var moveVelocity = VelocityFromMove(action, MOVE_SPEED);
    var pushVelocity = Pushable.Impulse;
    var velocity = moveVelocity;//+pushVelocity;

    Controller.Move(dt*velocity);

    transform.rotation = RotationFromInputs(transform, TURN_SPEED, action, dt);
    if (action.Jump.JustDown) {
      Cannon.DepressTrigger();
    } else if (action.Jump.Down) {
      Cannon.HoldTrigger();
    } else if (action.Jump.JustUp) {
      Cannon.ReleaseTrigger();
    }
  }
}