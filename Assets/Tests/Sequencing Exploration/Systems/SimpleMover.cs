using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleMover : MonoBehaviour {
  [Header("Reads From")]
  [SerializeField] LocalTime LocalTime;
  [Header("Writes To")]
  [SerializeField] CharacterController Controller;
  [SerializeField] Velocity Velocity;
  [SerializeField] Animator Animator;

  void FixedUpdate() {
    Controller.Move(LocalTime.FixedDeltaTime * Velocity.Value);
    Animator.SetFloat("YSpeed", Velocity.Value.y);
    Velocity.Value = Vector3.zero;
  }
}