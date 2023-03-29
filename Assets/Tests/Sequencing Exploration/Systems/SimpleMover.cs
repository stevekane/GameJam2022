using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleMover : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Gravity Gravity;
  [SerializeField] Velocity Velocity;
  [SerializeField] ActionsAndAI.JumpCount JumpCount;

  void FixedUpdate() {
    if (Controller.isGrounded && Velocity.Value.y < 0) {
      Velocity.Value.y = Time.deltaTime * Gravity.Value;
      // TODO: This could be a "land event"... not sure which is more robust?
      JumpCount.Value = 1;
    } else {
      Velocity.Value.y += Time.deltaTime * Gravity.Value;
    }
    Controller.Move(Time.deltaTime * Velocity.Value);
  }
}