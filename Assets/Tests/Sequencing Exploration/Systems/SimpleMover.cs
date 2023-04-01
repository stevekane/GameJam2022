using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleMover : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] MaxFallSpeed MaxFallSpeed;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Gravity Gravity;
  [SerializeField] Velocity Velocity;

  void Awake() => Debug.LogWarning("Restore SimpleMover capping fall speed");

  void FixedUpdate() {
    if (Controller.isGrounded) {
      SimpleAbilityManager.Tags.AddFlags(AbilityTag.CanJump);
      SimpleAbilityManager.Tags.AddFlags(AbilityTag.Grounded);
    } else {
      SimpleAbilityManager.Tags.ClearFlags(AbilityTag.Grounded);
    }
    if (Controller.isGrounded && Velocity.Value.y < 0) {
      Velocity.Value.y = LocalTime.FixedDeltaTime * Gravity.Value;
    } else {
      Velocity.Value.y += LocalTime.FixedDeltaTime * Gravity.Value;
    }
    // Velocity.Value.y = -Mathf.Min(Mathf.Abs(MaxFallSpeed.Value), Mathf.Abs(Velocity.Value.y));
    Controller.Move(LocalTime.FixedDeltaTime * Velocity.Value);
  }
}