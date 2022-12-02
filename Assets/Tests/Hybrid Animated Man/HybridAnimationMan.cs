using System.Collections;
using UnityEngine;

public class HybridAnimationMan : MonoBehaviour {
  [SerializeField] ButtonCode AttackButtonCode;
  [SerializeField] PlayableAnimation AttackAnimation;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;
  [SerializeField] Animator Animator;
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] Status Status;
  [SerializeField] Attributes Attributes;
  [SerializeField] CharacterController Controller;
  [SerializeField] float IdleThreshold = .1f;

  void Start() {
    InputManager.Instance.ButtonEvent(AttackButtonCode, ButtonPressType.JustDown).Listen(PlayAttack);
  }

  void OnDestroy() {
    InputManager.Instance.ButtonEvent(AttackButtonCode, ButtonPressType.JustDown).Unlisten(PlayAttack);
    StopAllCoroutines();
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  void PlayAttack() => StartCoroutine(Attack());

  IEnumerator Attack() {
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, transform.position+Vector3.up, transform.rotation, 1);
    yield return AnimationDriver.Play(AttackAnimation);
  }

  void FixedUpdate() {
    var xz = InputManager.Instance.AxisLeft.XZ;
    var aim = InputManager.Instance.AxisRight.XZ.TryGetDirection() ?? transform.forward;
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var velocity = moveSpeed*xz.normalized;
    var gravitationalForce = Time.fixedDeltaTime*Physics.gravity.y;
    var rotation = RotationFromDesired(transform, turnSpeed, aim);
    transform.rotation = rotation;
    if (Controller.isGrounded) {
      velocity.y = gravitationalForce;
    } else {
      velocity.y = gravitationalForce+Controller.velocity.y;
    }
    var localVelocity = Quaternion.Inverse(transform.rotation)*velocity;
    Controller.Move(Time.fixedDeltaTime*velocity);
    Animator.SetBool("Moving", velocity.sqrMagnitude > IdleThreshold);
    Animator.SetFloat("RightVelocity", localVelocity.x/moveSpeed);
    Animator.SetFloat("ForwardVelocity", localVelocity.z/moveSpeed);
  }
}