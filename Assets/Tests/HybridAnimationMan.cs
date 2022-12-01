using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class HybridAnimationMan : MonoBehaviour {
  [SerializeField] AnimationClip AttackClip;
  [SerializeField] Animator Animator;
  [SerializeField] Status Status;
  [SerializeField] Attributes Attributes;
  [SerializeField] CharacterController Controller;
  [SerializeField] float IdleThreshold = .1f;

  PlayableGraph AnimationGraph;

  void Start() {
    AnimationGraph = PlayableGraph.Create("Actions");
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(PlayAction);
  }

  void OnDestroy() {
    AnimationGraph.Destroy();
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(PlayAction);
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  static IEnumerator PlayAnimation(Animator animator, PlayableGraph graph, AnimationClip clip) {
    var clipPlayable = AnimationClipPlayable.Create(graph, clip);
    var output = AnimationPlayableOutput.Create(graph, clip.name, animator);
    clipPlayable.SetDuration(clip.length);
    clipPlayable.Play();
    graph.Play();
    output.SetSourcePlayable(clipPlayable);
    yield return Fiber.Until(() => clipPlayable.IsDone());
    graph.Stop();
    clipPlayable.Destroy();
  }

  IEnumerator Attack() {
    var disableMovementEffect = new ScriptedMovementEffect();
    Status.Add(disableMovementEffect);
    yield return PlayAnimation(Animator, AnimationGraph, AttackClip);
    Status.Remove(disableMovementEffect);
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

  void PlayAction() {
    StartCoroutine(Attack());
  }
}
