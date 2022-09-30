using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;

  CharacterController Controller;
  Attributes Attributes;
  Status Status;
  AbilityManager AbilityManager;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Attributes = GetComponent<Attributes>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var move = AbilityManager.GetAxis(AxisTag.Move).XZ;

    if (Status.CanRotate && move.magnitude > 0) {
      transform.forward = move;
    }
    if (Status.CanMove) {
      Controller.Move(dt*Attributes.GetValue(AttributeTag.MoveSpeed, MOVE_SPEED)*move+dt*Physics.gravity);
    }
  }
}