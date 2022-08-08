using UnityEngine;

public class AbilityMan : MonoBehaviour {
  public float MOVE_SPEED = 10;
  public AbilityManBaseAbility AbilityManBaseAbility;

  CharacterController Controller;
  Status Status;
  AbilityManager AbilityManager;

  void Start() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
    AbilityManager.TryRun(AbilityManBaseAbility);
    InputManager.Instance.L1.JustUp.Connected.Add(AbilityManager.L1JustUp);
    InputManager.Instance.L1.JustDown.Connected.Add(AbilityManager.L1JustDown);
    InputManager.Instance.L2.JustUp.Connected.Add(AbilityManager.L2JustUp);
    InputManager.Instance.L2.JustDown.Connected.Add(AbilityManager.L2JustDown);
    InputManager.Instance.R1.JustUp.Connected.Add(AbilityManager.R1JustUp);
    InputManager.Instance.R1.JustDown.Connected.Add(AbilityManager.R1JustDown);
    InputManager.Instance.R2.JustUp.Connected.Add(AbilityManager.R2JustUp);
    InputManager.Instance.R2.JustDown.Connected.Add(AbilityManager.R2JustDown);
  }

  void OnDestroy() {
    InputManager.Instance.L1.JustUp.Connected.Remove(AbilityManager.L1JustUp);
    InputManager.Instance.L1.JustDown.Connected.Remove(AbilityManager.L1JustDown);
    InputManager.Instance.L2.JustUp.Connected.Remove(AbilityManager.L2JustUp);
    InputManager.Instance.L2.JustDown.Connected.Remove(AbilityManager.L2JustDown);
    InputManager.Instance.R1.JustUp.Connected.Remove(AbilityManager.R1JustUp);
    InputManager.Instance.R1.JustDown.Connected.Remove(AbilityManager.R1JustDown);
    InputManager.Instance.R2.JustUp.Connected.Remove(AbilityManager.R2JustUp);
    InputManager.Instance.R2.JustDown.Connected.Remove(AbilityManager.R2JustDown);
  }

  void FixedUpdate() {
    var action = Inputs.Action;
    var dt = Time.fixedDeltaTime;
    var move = action.Left.XZ;

    if (Status.CanRotate && move.magnitude > 0) {
      transform.forward = move;
    }
    if (Status.CanMove) {
      Controller.Move(dt*MOVE_SPEED*Inputs.Action.Left.XZ+dt*Physics.gravity);
    }
  }
}