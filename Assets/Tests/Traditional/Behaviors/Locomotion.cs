using UnityEngine;

namespace Traditional {
  public class Locomotion : MonoBehaviour {
    [SerializeField] Animator Animator;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] AnimationTimeScale AnimationTimeScale;
    [SerializeField] Grounded Grounded;

    void FixedUpdate() {
      var inputVelocity = LocalTimeScale.Value * MoveSpeed.Value * MoveDirection.Value;
      var orientedVelocity = Quaternion.Inverse(transform.rotation)*inputVelocity;
      Animator.SetFloat("RightVelocity", orientedVelocity.x * AnimationTimeScale.Value);
      Animator.SetFloat("ForwardVelocity", orientedVelocity.z * AnimationTimeScale.Value);
      Animator.SetBool("IsGrounded", Grounded.Value);
      Animator.SetSpeed(LocalTimeScale.Value);
    }
  }
}