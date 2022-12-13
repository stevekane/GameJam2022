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
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*Mover.Velocity;
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    animator.SetFloat("RightVelocity", moveSpeed > 0 ? orientedVelocity.x/moveSpeed : 0);
    animator.SetFloat("ForwardVelocity", moveSpeed > 0 ? orientedVelocity.z/moveSpeed : 0);
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsHurt", Status.IsHurt);
    // TorsoRotationConstraint.weight = Mathf.MoveTowards(TorsoRotationConstraint.weight, Status.IsHurt ? 0 : 1, 1/(float)ConstraintBlendFrames);
    TorsoRotationConstraint.weight = 0;
    HeadAimConstraint.weight = 0;
    // Use localtimescale to slow the AnimationDriver
    var localSpeed = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);
    if (localSpeed < 1) {
      AnimationDriver.SetSpeed(localSpeed);
    } else {
      AnimationDriver.SetSpeed(AnimationDriver.BaseSpeed);
    }
  }
}