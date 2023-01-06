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

      // broadcast events for physics.... probably should not live here?
      // TODO: This needs updating. This hack of lookin at Animator was just for testing (it works)
      if (Animator.GetBool("IsGrounded") && !CharacterController.isGrounded) {
        SendMessage(Globals.TAKEOFF_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }
      if (!Animator.GetBool("IsGrounded") && CharacterController.isGrounded) {
        SendMessage(Globals.LAND_EVENT_NAME, SendMessageOptions.DontRequireReceiver);
      }

      // animation
      var inputVelocity = LocalTimeScale.Value * MoveSpeed.Value * MoveDirection.Value;
      var orientedVelocity = Quaternion.Inverse(transform.rotation)*inputVelocity;
      Animator.SetFloat("RightVelocity", orientedVelocity.x * animationTimeScale);
      Animator.SetFloat("ForwardVelocity", orientedVelocity.z * animationTimeScale);
      Animator.SetBool("IsGrounded", CharacterController.isGrounded);
      Animator.SetSpeed(localTimeScale < 1 ? localTimeScale : 1);
    }
  }
}