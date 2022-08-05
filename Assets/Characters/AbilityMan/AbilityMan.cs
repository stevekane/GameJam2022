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
    InputManager.Instance.R1.JustDown.Action += delegate {
      AbilityManager.OnAbilityAction(AbilityAction.R1JustDown);
    };
    InputManager.Instance.R2.JustDown.Action += delegate {
      AbilityManager.OnAbilityAction(AbilityAction.R2JustDown);
    };
  }

  void OnDestroy() {
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