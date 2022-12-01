using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[Serializable]
public class PlayableAnimation {
  public AnimationClip Clip;
  public AvatarMask Mask = null;
  public float Speed = 1;
}

public class HybridAnimationMan : MonoBehaviour {
  [SerializeField] PlayableAnimation AttackAnimation;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;
  [SerializeField] Animator Animator;
  [SerializeField] Status Status;
  [SerializeField] Attributes Attributes;
  [SerializeField] CharacterController Controller;
  [SerializeField] float IdleThreshold = .1f;

  PlayableGraph AnimationGraph;

  void Start() {
    AnimationGraph = PlayableGraph.Create("Actions");
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(PlayAction);
    InputManager.Instance.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(PlayRunningAction);
  }

  void OnDestroy() {
    AnimationGraph.Destroy();
    InputManager.Instance.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(PlayAction);
    InputManager.Instance.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(PlayAction);
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  static IEnumerator PlayAnimation(Animator animator, PlayableGraph graph, PlayableAnimation animation) {
    var clipPlayable = AnimationClipPlayable.Create(graph, animation.Clip);
    var output = AnimationPlayableOutput.Create(graph, animation.Clip.name, animator);
    clipPlayable.SetSpeed(animation.Speed);
    clipPlayable.SetDuration(animation.Clip.length);
    output.SetSourcePlayable(clipPlayable);
    graph.Play();
    yield return clipPlayable.UntilDone();
    graph.Stop();
    clipPlayable.Destroy();
  }

  /*
  AnimController -
                  |- mixer (with mask) - Animator
  Clip -----------
  */
  static IEnumerator BlendAnimation(Animator animator, PlayableGraph graph, PlayableAnimation animation) {
    var output = AnimationPlayableOutput.Create(graph, animation.Clip.name, animator);
    var clipPlayable = AnimationClipPlayable.Create(graph, animation.Clip);
    var animatorPlayable = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
    var mixerPlayable = AnimationLayerMixerPlayable.Create(graph, 2);
    mixerPlayable.ConnectInput(0, animatorPlayable, 0, 1);
    mixerPlayable.ConnectInput(1, clipPlayable, 0, 1); // TODO: Probably should animate this mix level to crossfade
    mixerPlayable.SetLayerMaskFromAvatarMask(1, animation.Mask);
    clipPlayable.SetSpeed(animation.Speed);
    clipPlayable.SetDuration(animation.Clip.length);
    output.SetSourcePlayable(mixerPlayable);
    graph.Play();
    yield return clipPlayable.UntilDone();
    graph.Stop();
    clipPlayable.Destroy();
    mixerPlayable.Destroy();
    animatorPlayable.Destroy();
  }

  void PlayAction() => StartCoroutine(Attack());
  void PlayRunningAction() => StartCoroutine(RunningAttack());

  IEnumerator Attack() {
    var disableMovementEffect = new ScriptedMovementEffect();
    Status.Add(disableMovementEffect);
    yield return PlayAnimation(Animator, AnimationGraph, AttackAnimation);
    Status.Remove(disableMovementEffect);
  }

  IEnumerator RunningAttack() {
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, transform.position+Vector3.up, transform.rotation, 1);
    yield return BlendAnimation(Animator, AnimationGraph, AttackAnimation);
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