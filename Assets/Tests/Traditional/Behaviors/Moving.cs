using UnityEngine;

namespace Traditional {
  public class Moving : MonoBehaviour {
    [SerializeField] CharacterController CharacterController;
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] MoveDelta MoveDelta;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] FallSpeed FallSpeed;

    void FixedUpdate() {
      var dt = LocalTimeScale.Value * Time.fixedDeltaTime;
      var inputVelocity = LocalTimeScale.Value * MoveSpeed.Value * MoveDirection.Value;
      var inputDelta = dt * inputVelocity;
      var fallDelta = dt * FallSpeed.Value * Vector3.up;
      CharacterController.Move(inputDelta + fallDelta + MoveDelta.Value);
    }
  }
}