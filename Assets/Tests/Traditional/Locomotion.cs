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
      var moveDelta = MoveDelta.Evaluate(MoveDelta.Base);
      var moveDirection = MoveDirection.Evaluate(MoveDirection.Base);
      var aimDirection = AimDirection.Evaluate(AimDirection.Base);
      var moveSpeed = MoveSpeed.Evaluate(MoveSpeed.Base);
      var turnSpeed = TurnSpeed.Evaluate(TurnSpeed.Base);
      var gravity = Gravity.Evaluate(Gravity.Base);
      var localTimeScale = LocalTimeScale.Evaluate(LocalTimeScale.Base);
      var fallSpeed = FallSpeed.Evaluate(FallSpeed.Base);
      var maxFallSpeed = MaxFallSpeed.Evaluate(MaxFallSpeed.Base);
      var animationTimeScale = AnimationTimeScale.Evaluate(AnimationTimeScale.Base);
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
      FallSpeed.Base = Mathf.Min(maxFallSpeed, dt*gravity + (CharacterController.isGrounded ? 0 : FallSpeed.Base));
      MoveDelta.Base = Vector3.zero;
    }
  }
}