using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleMover : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] Gravity Gravity;
  [SerializeField] Velocity Velocity;

  void FixedUpdate() {
    if (Controller.isGrounded) {
      SimpleAbilityManager.Tags.AddFlags(AbilityTag.CanJump);
      SimpleAbilityManager.Tags.AddFlags(AbilityTag.Grounded);
    } else {
      SimpleAbilityManager.Tags.ClearFlags(AbilityTag.Grounded);
    }
    if (Controller.isGrounded && Velocity.Value.y < 0) {
      Velocity.Value.y = Time.deltaTime * Gravity.Value;
    } else {
      Velocity.Value.y += Time.deltaTime * Gravity.Value;
    }
    Controller.Move(Time.deltaTime * Velocity.Value);
  }
}