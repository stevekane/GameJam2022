using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

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
  [SerializeField] float VaultSpeedMultiplier = 1.25f;
  [SerializeField] float Gravity = -20f;
  [FormerlySerializedAs("TurnAndThrowDuration")]
  [SerializeField] Timeval WindupDuration = Timeval.FromMillis(250);
  [SerializeField] Timeval ThrowDuration = Timeval.FromMillis(100);
  [SerializeField] Timeval VaultDuration = Timeval.FromSeconds(1);
  [Tooltip("Strength of the arm constraint over the duration of pulling")]
  [SerializeField] AnimationCurve ArmPullConstraintStrength;
  [SerializeField] AudioSource PullLoop;
  [SerializeField] AudioSource ClipSource;
  [SerializeField] AudioClip ThrowClip;
  [SerializeField] AudioClip HitClip;
  [SerializeField] AudioClip VaultClip;
  [SerializeField] GameObject ThrowVFX;
  [SerializeField] GameObject HitVFX;
  [SerializeField] GameObject VaultVFX;

  IEnumerator Start() {
    var origin = transform.position;
    while (true) {
      var target = Random.onUnitSphere * Distance;
      yield return StartCoroutine(GrappleTo(target));
      yield return StartCoroutine(GrappleTo(origin));
    }
  }

  IEnumerator GrappleTo(Vector3 position) {
    Target.position = position;
    yield return StartCoroutine(Windup());
    yield return StartCoroutine(Throw());
    yield return StartCoroutine(Pull());
    yield return StartCoroutine(Vault());
  }

  IEnumerator Windup() {
    LineRenderer.enabled = false;
    Animator.SetInteger("GrappleState", 1);
    RightArmRig.weight = 0;
    var velocity  = CharacterController.velocity;
    for (var i = 0; i < WindupDuration.Ticks; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var delta = Target.position-transform.position;
      var toTargetXZ = delta.XZ().normalized;
      var atTargetXZ = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, atTargetXZ, degrees);
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Throw() {
    LineRenderer.enabled = true;
    Animator.SetInteger("GrappleState", 1);
    ClipSource.PlayOneShot(ThrowClip);
    Destroy(Instantiate(ThrowVFX, RightHandAvatarTransform.Transform.position, transform.rotation), 3);
    var velocity  = CharacterController.velocity;
    for (var i = 0; i <= ThrowDuration.Ticks; i++) {
      var interpolant = (float)i/(float)ThrowDuration.Ticks;
      var origin = RightHandAvatarTransform.Transform.position;
      var destination = Target.position;
      var armInterpolant = (float)i/(float)ThrowDuration.Ticks;
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      RightArmRig.weight = interpolant;
      LineRenderer.SetPosition(1, Vector3.Lerp(origin, destination, interpolant));
      yield return new WaitForFixedUpdate();
    }
    ClipSource.PlayOneShot(HitClip);
    Destroy(Instantiate(HitVFX, Target.position, transform.rotation), 3);
  }

  IEnumerator Pull() {
    var origin = transform.position;
    var delta = Target.position-transform.position;
    var distanceToTarget = delta.magnitude;
    var toTarget = delta.normalized;
    var toTargetXZ = delta.XZ().normalized;
    LineRenderer.enabled = true;
    Animator.SetInteger("GrappleState", 2);
    Debug.DrawRay(transform.position, toTarget, Color.blue, 3f);
    Debug.DrawRay(transform.position, transform.up, Color.green, 3f);
    Animator.SetFloat("Rotation", Vector3.Dot(toTarget, transform.up));
    PullLoop.Play();
    var duration = distanceToTarget / PullSpeed;
    for (var i = 0; i <= duration; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var positionInterpolant = (float)i/(float)duration;
      var nextPosition = Vector3.Lerp(origin, Target.position, positionInterpolant);
      var atTargetXZ = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, atTargetXZ, degrees);
      CharacterController.Move(nextPosition-transform.position);
      RightArmRig.weight = ArmPullConstraintStrength.Evaluate(positionInterpolant);
      yield return new WaitForFixedUpdate();
    }
    RightArmRig.weight = 0;
    PullLoop.Stop();
  }

  IEnumerator Vault() {
    LineRenderer.enabled = false;
    Animator.SetInteger("GrappleState", 3);
    ClipSource.PlayOneShot(VaultClip);
    RightArmRig.weight = 0;
    Destroy(Instantiate(VaultVFX, transform.position, transform.rotation), 3);
    var velocity = CharacterController.velocity * VaultSpeedMultiplier;
    for (var i = 0; i < VaultDuration.Ticks; i++) {
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      yield return new WaitForFixedUpdate();
    }
  }

  void LateUpdate() {
    LineRenderer.SetPosition(0, RightHandAvatarTransform.Transform.position);
    Time.timeScale = TimeScale;
  }
}