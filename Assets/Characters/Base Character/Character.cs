using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(Attributes))]
[RequireComponent(typeof(Status))]
[RequireComponent(typeof(Upgrades))]
[RequireComponent(typeof(Damage))]
[RequireComponent(typeof(AnimationDriver))]
[RequireComponent(typeof(Mover))]
public class Character : MonoBehaviour {
  [field:SerializeField] public AbilityManager AbilityManager { get; private set; }
  [field:SerializeField] public Attributes Attributes { get; private set; }
  [field:SerializeField] public Status Status { get; private set; }
  [field:SerializeField] public Upgrades Upgrades { get; private set; }
  [field:SerializeField] public Damage Damage { get; private set; }
  [field:SerializeField] public AnimationDriver AnimationDriver { get; private set; }
  [field:SerializeField] public Mover Mover { get; private set; }
  [field:SerializeField] public MultiRotationConstraint TorsoRotationConstraint { get; private set; }
  [field:SerializeField] public MultiAimConstraint HeadAimConstraint { get; private set; }

  [SerializeField] float IdleThreshold = 0.1f;
  [SerializeField] int ConstraintBlendFrames = 4;

  void FixedUpdate() {
    var animator = AnimationDriver.Animator;
    var velocity = Mover.Velocity;
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*Mover.Velocity;
    // We want to continue to animate as if we were not slowed
    if (moveSpeed > 0 && Mover.Velocity.XZ().magnitude > IdleThreshold) {
      animator.SetBool("Moving", true);
      animator.SetFloat("RightVelocity", orientedVelocity.x/moveSpeed);
      animator.SetFloat("ForwardVelocity", orientedVelocity.z/moveSpeed);
    } else {
      animator.SetBool("Moving", false);
      animator.SetFloat("RightVelocity", 0);
      animator.SetFloat("ForwardVelocity", 0);
    }
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsHurt", Status.IsHurt);
    TorsoRotationConstraint.weight = Mathf.MoveTowards(TorsoRotationConstraint.weight, Status.IsHurt ? 0 : 1, 1/(float)ConstraintBlendFrames);
    HeadAimConstraint.weight = 0;
  }
}