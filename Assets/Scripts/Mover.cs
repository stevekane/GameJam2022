using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Status), typeof(AbilityManager))]
public class Mover : MonoBehaviour {
  public static void UpdateAxes(AbilityManager manager, Vector3 desiredMoveDir, Vector3 desiredFacing) {
    manager.GetAxis(AxisTag.Move).Update(0f, new Vector2(desiredMoveDir.x, desiredMoveDir.z));
    manager.GetAxis(AxisTag.Aim).Update(0f, new Vector2(desiredFacing.x, desiredFacing.z));
  }

  public static void GetAxes(AbilityManager manager, out Vector3 desiredMoveDir, out Vector3 desiredFacing) {
    desiredMoveDir = manager.GetAxis(AxisTag.Move).XZ;
    desiredFacing = manager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? manager.transform.forward;
  }

  public static IEnumerator Face(Transform t, Vector3 direction, float degreesPerSecond, float threshold) {
    while (Vector3.Dot(t.forward, direction) < threshold) {
      var current = t.forward.XZ();
      var radiansPerSecond = degreesPerSecond*Mathf.Deg2Rad*Time.fixedDeltaTime;
      var next = Vector3.RotateTowards(t.forward, direction, radiansPerSecond, 0);
      t.rotation = Quaternion.LookRotation(next, Vector3.up);
      yield return null;
    }
  }

  public float Gravity;

  Vector3 Velocity;

  CharacterController Controller;
  Attributes Attributes;
  Status Status;
  AbilityManager AbilityManager;

  void Awake() {
    Controller = GetComponent<CharacterController>();
    Attributes = GetComponent<Attributes>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  void FixedUpdate() {
    GetAxes(AbilityManager, out var desiredMoveDir, out var desiredFacing);

    var moveVelocity = Attributes.GetValue(AttributeTag.MoveSpeed) * desiredMoveDir;
    Velocity.SetXZ(moveVelocity);
    var gravity = Time.fixedDeltaTime * Gravity;
    Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
    if (!Status.HasGravity)
      Velocity.y = 0f;
    Controller.Move(Time.fixedDeltaTime * Velocity);

    transform.rotation = RotationFromDesired(transform, Attributes.GetValue(AttributeTag.TurnSpeed), desiredFacing);
  }
}
