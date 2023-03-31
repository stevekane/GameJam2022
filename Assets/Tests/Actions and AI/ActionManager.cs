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

    /*
    An idea:

      All capabilities of an actor are represented by stateful scripts.
      These scripts may be running.

      They are marked by two possible systems:

        GuardClause (arbitrary closure over state)
        TagSet (set of tags describing how this ability starts and stops)

      An ability is said to be able to be run if its guardclause is true
      and it is not blocked by the abilitysystem due to its tags.

      All raw inputs are mapped to methods on abilities.
      An ability may be synchronous meaning it only defines a start method.
    */

    void FixedUpdate() {
      StartAimAction.IsAvailable = Controller.isGrounded && !Aiming.Value;
      GroundControl.IsAvailable = Controller.isGrounded;
      AirControl.IsAvailable = !Controller.isGrounded;
    }
  }
}