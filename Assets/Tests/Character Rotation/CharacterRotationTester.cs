using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CharacterRotationTester : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] CharacterController CharacterController;
  [SerializeField] AvatarTransform RightHandAvatarTransform;
  [SerializeField] Rig RightArmRig;
  [SerializeField] LineRenderer LineRenderer;
  [SerializeField] Animator Animator;
  [SerializeField] float PullSpeed = 15;
  [SerializeField] float TimeScale = 1;
  [SerializeField] float TurnSpeed = 360;
  [SerializeField] float Distance = 5;
  [SerializeField] Timeval TurnAndThrowDuration = Timeval.FromMillis(250);
  [SerializeField] Timeval ThrowDuration = Timeval.FromMillis(100);
  [SerializeField] Timeval PullStretchDuration = Timeval.FromMillis(250);
  [SerializeField] Timeval VaultDuration = Timeval.FromSeconds(1);

  Vector3 ToTarget => Target.position-transform.position;
  Quaternion AtTarget => Quaternion.LookRotation(ToTarget, Vector3.up);

  IEnumerator Start() {
    LineRenderer.enabled = true;
    var origin = transform.position;
    while (true) {
      var target = Random.onUnitSphere * Distance;
      yield return StartCoroutine(GrappleTo(target));
      yield return StartCoroutine(GrappleTo(origin));
    }
  }

  IEnumerator GrappleTo(Vector3 position) {
    Target.position = position;
    yield return StartCoroutine(TurnAndThrow());
    yield return StartCoroutine(Throw());
    yield return StartCoroutine(Pull());
    yield return StartCoroutine(Vault());
  }

  IEnumerator TurnAndThrow() {
    LineRenderer.enabled = false;
    Animator.SetInteger("GrappleState", 1);
    var speed = CharacterController.velocity;
    for (var i = 0; i < TurnAndThrowDuration.Ticks; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var interpolant = Mathf.InverseLerp(PullStretchDuration.Ticks, 0, i);
      CharacterController.Move(Time.fixedDeltaTime*speed);
      Animator.SetFloat("Blend", 90 * interpolant);
      RightArmRig.weight = interpolant;
      transform.rotation = Quaternion.RotateTowards(transform.rotation, AtTarget, degrees);
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Throw() {
    LineRenderer.enabled = true;
    Animator.SetInteger("GrappleState", 1);
    for (var i = 0; i <= ThrowDuration.Ticks; i++) {
      var interpolant = (float)i/(float)ThrowDuration.Ticks;
      var origin = RightHandAvatarTransform.Transform.position;
      var destination = Target.position;
      LineRenderer.SetPosition(1, Vector3.Lerp(origin, destination, interpolant));
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Pull() {
    LineRenderer.enabled = true;
    Animator.SetInteger("GrappleState", 2);
    var origin = transform.position;
    var distanceToTarget = Vector3.Distance(transform.position, Target.position);
    var duration = distanceToTarget / PullSpeed;
    Debug.Log(duration);
    for (var i = 0; i <= duration; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var stretchInterpolant = Mathf.InverseLerp(0, PullStretchDuration.Ticks, i);
      var positionInterpolant = (float)i/(float)duration;
      var nextPosition = Vector3.Lerp(origin, Target.position, positionInterpolant);
      CharacterController.Move(nextPosition-transform.position);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, AtTarget, degrees);
      Animator.SetFloat("Blend", 90 * stretchInterpolant);
      RightArmRig.weight = 1;
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Vault() {
    LineRenderer.enabled = false;
    Animator.SetInteger("GrappleState", 3);
    var speed = CharacterController.velocity;
    for (var i = 0; i < VaultDuration.Ticks; i++) {
      var interpolant = Mathf.InverseLerp(PullStretchDuration.Ticks, 0, i);
      CharacterController.Move(Time.fixedDeltaTime*speed);
      RightArmRig.weight = interpolant;
      yield return new WaitForFixedUpdate();
    }
  }

  void LateUpdate() {
    LineRenderer.SetPosition(0, RightHandAvatarTransform.Transform.position);
    Time.timeScale = TimeScale;
  }
}