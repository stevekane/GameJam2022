using UnityEngine;

namespace ActionsAndAI {
  public class Jump : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    [SerializeField] Velocity Velocity;
    [SerializeField] float Strength = 15;
    public void OnStart() => Velocity.Value.y = Strength;
  }
}