using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleMover : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Gravity Gravity;
  [SerializeField] Velocity Velocity;

  void FixedUpdate() {
    if (Controller.isGrounded && Velocity.Value.y < 0) {
      Velocity.Value.y = Time.deltaTime * Gravity.Value;
    } else {
      Velocity.Value.y += Time.deltaTime * Gravity.Value;
    }
    Controller.Move(Time.deltaTime * Velocity.Value);
  }
}