using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Late)]
  public class ActionManager : MonoBehaviour {
    [Header("Actions")]
    [SerializeField] ActionEventSource StartAimAction;
    [SerializeField] ActionEventSource Jump;
    [SerializeField] ActionEventSource DoubleJump;
    [SerializeField] ActionEventSourceVector3 GroundControl;
    [SerializeField] ActionEventSourceVector3 AirControl;

    [Header("State")]
    [SerializeField] Aiming Aiming;
    [SerializeField] JumpCount JumpCount;
    [SerializeField] CharacterController Controller;

    void FixedUpdate() {
      StartAimAction.IsAvailable = Controller.isGrounded && !Aiming.Value;
      Jump.IsAvailable = Controller.isGrounded;
      DoubleJump.IsAvailable = !Controller.isGrounded && JumpCount.Value > 0;
      GroundControl.IsAvailable = Controller.isGrounded;
      AirControl.IsAvailable = !Controller.isGrounded;
    }
  }
}