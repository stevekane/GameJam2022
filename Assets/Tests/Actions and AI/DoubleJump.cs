using UnityEngine;

namespace ActionsAndAI {
  public class DoubleJump : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    [SerializeField] Velocity Velocity;
    [SerializeField] JumpCount JumpCount;
    [SerializeField] float Strength = 35;
    public void OnStart() {
      JumpCount.Value--;
      Velocity.Value.y = Strength;
    }
  }
}