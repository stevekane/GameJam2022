using System.Collections;
using UnityEngine;

public class CharacterRotationTester : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] CharacterController CharacterController;
  [SerializeField] AvatarTransform HandAvatarTransform;
  [SerializeField] LineRenderer LineRenderer;
  [SerializeField] Animator Animator;
  [Tooltip("Rate of pulling in units per second")]
  [SerializeField] float PullSpeed = 15;
  [Tooltip("Rate of yaw change in degrees per second")]
  [SerializeField] float TurnSpeed = 360;
  [Tooltip("Rate of pitch change in degrees/second")]
  [SerializeField] float RotationSpeed = 180;
  [SerializeField] float Distance = 5;
  [SerializeField] float VaultSpeedMultiplier = 1.25f;
  [SerializeField] float Gravity = -20f;
  [SerializeField] Timeval WindupDuration = Timeval.FromMillis(250);
  [SerializeField] Timeval ThrowDuration = Timeval.FromMillis(100);
  [SerializeField] Timeval VaultDuration = Timeval.FromSeconds(1);
  [SerializeField] AudioSource PullLoop;
  [SerializeField] AudioSource ClipSource;
  [SerializeField] AudioClip ThrowClip;
  [SerializeField] AudioClip HitClip;
  [SerializeField] AudioClip VaultClip;
  [SerializeField] GameObject ThrowVFX;
  [SerializeField] GameObject HitVFX;
  [SerializeField] GameObject VaultVFX;

  float CurrentRotation = 0;
  float NormalizedRotationSpeed => RotationSpeed / 90;

  /*
  TODO: Express rotation in degrees/second.

  Currently, rotation is expressed in something like... normalized units (except the range is -1 to 1)

  Dot(Vnorm,Up) is a value between -1 and 1.

  This means that the range is 180 degrees.

  Thus, if we express rotationspeed in degrees/second then we must map that into our normalized range.

  For example, if the value is 180 then we expect to rotate 2 units/second which could for example
  carry you from -1 to 1.

  If the value is 360 then we expect to rotate 4 units/second meaning we could get from -1 to 1 in half a second.

  Therefore, to convert from Degrees/second to dotvalue/second we must divide the value by 90.

  180 / 90 = 2 u/s
  360 / 90 = 4 u/s etc
  */

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
    var velocity  = CharacterController.velocity;
    for (var i = 0; i < WindupDuration.Ticks; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var delta = Target.position-transform.position;
      var toTarget = delta.normalized;
      var toTargetXZ = delta.XZ().normalized;
      var atTargetXZ = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, atTargetXZ, degrees);
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      CurrentRotation = Mathf.MoveTowards(CurrentRotation, Vector3.Dot(toTarget, Vector3.up), Time.fixedDeltaTime * NormalizedRotationSpeed);
      Animator.SetFloat("Rotation", CurrentRotation);
      yield return new WaitForFixedUpdate();
    }
  }

  IEnumerator Throw() {
    LineRenderer.enabled = true;
    Animator.SetInteger("GrappleState", 1);
    ClipSource.PlayOneShot(ThrowClip);
    Destroy(Instantiate(ThrowVFX, HandAvatarTransform.Transform.position, transform.rotation), 3);
    var velocity = CharacterController.velocity;
    for (var i = 0; i <= ThrowDuration.Ticks; i++) {
      var interpolant = (float)i/(float)ThrowDuration.Ticks;
      var origin = HandAvatarTransform.Transform.position;
      var destination = Target.position;
      var delta = Target.position-transform.position;
      var toTarget = delta.normalized;
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      LineRenderer.SetPosition(1, Vector3.Lerp(origin, destination, interpolant));
      CurrentRotation = Mathf.MoveTowards(CurrentRotation, Vector3.Dot(toTarget, Vector3.up), Time.fixedDeltaTime * NormalizedRotationSpeed);
      Animator.SetFloat("Rotation", CurrentRotation);
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
    PullLoop.Play();
    var duration = distanceToTarget / PullSpeed;
    for (var i = 0; i <= duration; i++) {
      var degrees = TurnSpeed * Time.fixedDeltaTime;
      var interpolant = (float)i/(float)duration;
      var nextPosition = Vector3.Lerp(origin, Target.position, interpolant);
      var atTargetXZ = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, atTargetXZ, degrees);
      CharacterController.Move(nextPosition-transform.position);
      CurrentRotation = Mathf.MoveTowards(CurrentRotation, Vector3.Dot(toTarget, Vector3.up), Time.fixedDeltaTime * NormalizedRotationSpeed);
      Animator.SetFloat("Rotation", CurrentRotation);
      yield return new WaitForFixedUpdate();
    }
    PullLoop.Stop();
  }

  IEnumerator Vault() {
    LineRenderer.enabled = false;
    Animator.SetInteger("GrappleState", 3);
    ClipSource.PlayOneShot(VaultClip);
    Destroy(Instantiate(VaultVFX, transform.position, transform.rotation), 3);
    var velocity = CharacterController.velocity * VaultSpeedMultiplier;
    for (var i = 0; i < VaultDuration.Ticks; i++) {
      velocity += Time.fixedDeltaTime * Gravity * Vector3.up;
      CharacterController.Move(Time.fixedDeltaTime * velocity);
      CurrentRotation = Mathf.MoveTowards(CurrentRotation, Vector3.Dot(velocity.normalized, Vector3.up), Time.fixedDeltaTime * NormalizedRotationSpeed);
      Animator.SetFloat("Rotation", CurrentRotation);
      yield return new WaitForFixedUpdate();
    }
  }

  void LateUpdate() {
    LineRenderer.SetPosition(0, HandAvatarTransform.Transform.position);
  }
}