using System.Collections;
using UnityEngine;

public class HybridAnimationMan : MonoBehaviour {
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
    var animator = GetComponent<Animator>();
    var attributes = GetComponent<Attributes>();
    var controller = GetComponent<CharacterController>();
    var moveSpeed = attributes.GetValue(AttributeTag.MoveSpeed);
    var turnSpeed = attributes.GetValue(AttributeTag.TurnSpeed);
    var velocity = moveSpeed*xz;
    var gravitationalForce = Time.fixedDeltaTime*Physics.gravity.y;
    var rotation = RotationFromDesired(transform, turnSpeed, aim);
    transform.rotation = rotation;
    if (controller.isGrounded) {
      velocity.y = gravitationalForce;
    } else {
      velocity.y = gravitationalForce+controller.velocity.y;
    }
    var forwardSpeed = Vector3.Dot(velocity, transform.forward)/moveSpeed;
    var rightSpeed = Vector3.Dot(velocity, transform.right)/moveSpeed;
    var moving = Mathf.Abs(forwardSpeed) > 0 || Mathf.Abs(rightSpeed) > 0;
    controller.Move(Time.fixedDeltaTime*velocity);
    animator.SetBool("Moving", moving);
    animator.SetFloat("RightVelocity", rightSpeed);
    animator.SetFloat("ForwardVelocity", forwardSpeed);
  }

  void PlayAction() {
    Debug.Log("Action");
  }

  IEnumerator FlipFlop() {
    yield return new WaitForSeconds(1);
    Debug.Log("Blap blap");
  }
}
