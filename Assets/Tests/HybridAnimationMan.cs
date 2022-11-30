using System.Collections;
using UnityEngine;

public class HybridAnimationMan : MonoBehaviour {
  [SerializeField] Animator Animator;

  public float IdleThreshold = .1f;
  void Start() {
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(PlayAction);
  }

  void OnDestroy() {
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(PlayAction);
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  void FixedUpdate() {
    var xz = InputManager.Instance.AxisLeft.XZ;
    var aim = InputManager.Instance.AxisRight.XZ.TryGetDirection() ?? transform.forward;
    var attributes = GetComponent<Attributes>();
    var controller = GetComponent<CharacterController>();
    var moveSpeed = attributes.GetValue(AttributeTag.MoveSpeed);
    var turnSpeed = attributes.GetValue(AttributeTag.TurnSpeed);
    var velocity = moveSpeed*xz.normalized;
    var gravitationalForce = Time.fixedDeltaTime*Physics.gravity.y;
    var rotation = RotationFromDesired(transform, turnSpeed, aim);
    transform.rotation = rotation;
    if (controller.isGrounded) {
      velocity.y = gravitationalForce;
    } else {
      velocity.y = gravitationalForce+controller.velocity.y;
    }
    var localVelocity = Quaternion.Inverse(transform.rotation)*velocity;
    controller.Move(Time.fixedDeltaTime*velocity);
    Animator.SetBool("Moving", velocity.sqrMagnitude > IdleThreshold);
    Animator.SetFloat("RightVelocity", localVelocity.x/moveSpeed);
    Animator.SetFloat("ForwardVelocity", localVelocity.z/moveSpeed);
  }

  void PlayAction() {
    Debug.Log("Action");
  }

  IEnumerator FlipFlop() {
    yield return new WaitForSeconds(1);
    Debug.Log("Blap blap");
  }
}
