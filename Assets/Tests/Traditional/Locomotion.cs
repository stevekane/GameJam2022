using UnityEngine;

namespace Traditional {
  public class Locomotion : MonoBehaviour {
    [SerializeField] CharacterController CharacterController;
    [SerializeField] Animator Animator;
    [SerializeField] MoveDelta MoveDelta;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] AimDirection AimDirection;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] TurnSpeed TurnSpeed;
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] Gravity Gravity;
    [SerializeField] FallSpeed FallSpeed;
    [SerializeField] MaxFallSpeed MaxFallSpeed;
    [SerializeField] AnimationTimeScale AnimationTimeScale;

    void FixedUpdate() {
      var moveDelta = MoveDelta.Value;
      var moveDirection = MoveDirection.Value;
      var aimDirection = AimDirection.Value;
      var moveSpeed = MoveSpeed.Value;
      var turnSpeed = TurnSpeed.Value;
      var gravity = Gravity.Value;
      var localTimeScale = LocalTimeScale.Value;
      var fallSpeed = FallSpeed.Value;
      var maxFallSpeed = MaxFallSpeed.Value;
      var animationTimeScale = AnimationTimeScale.Value;
      var dt = localTimeScale*Time.fixedDeltaTime;

      // movement
      var inputVelocity = localTimeScale * moveSpeed * moveDirection;
      var inputDelta = dt * inputVelocity;
      var fallDelta = dt * fallSpeed * Vector3.up;
      CharacterController.Move(inputDelta + fallDelta + moveDelta);

      // turning
      var maxDegrees = dt * localTimeScale * turnSpeed;
      var desiredRotation = Quaternion.LookRotation(aimDirection.sqrMagnitude > 0 ? aimDirection : transform.forward);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maxDegrees);

      // animation
      var orientedVelocity = Quaternion.Inverse(transform.rotation)*inputVelocity;
      Animator.SetFloat("RightVelocity", orientedVelocity.x * animationTimeScale);
      Animator.SetFloat("ForwardVelocity", orientedVelocity.z * animationTimeScale);
      Animator.SetBool("IsGrounded", CharacterController.isGrounded);
      Animator.SetSpeed(localTimeScale < 1 ? localTimeScale : 1);

      // Write next frames state here... ponder this...
      if (!CharacterController.isGrounded) {
        var unboundedFallSpeed = dt * gravity + fallSpeed;
        var boundedFallSpeed = Mathf.Max(maxFallSpeed, unboundedFallSpeed);
        FallSpeed.Add(boundedFallSpeed);
      } else {
        FallSpeed.Add(dt * gravity);
      }
    }
  }
}