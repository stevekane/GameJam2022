using System.Threading.Tasks;
using UnityEngine;

public class Grapple : Ability {
  [SerializeField] AvatarTransform HookOrigin;
  [SerializeField] LineRenderer GrappleLine;
  [SerializeField] Animator Animator;
  [SerializeField] float PullSpeed = 15;
  [SerializeField] float TurnSpeed = 360;
  [SerializeField] float RotationSpeed = 2; // TODO: This probably should be degrees/second
  [SerializeField] float VaultSpeedMultiplier = 1.25f;
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

  GrapplePoint Candidate;
  GrapplePoint Target;
  Vector3 Velocity;
  float CurrentRotation;

  InlineEffect ActiveEffect => new(s => {
    s.CanRotate = false;
    s.CanMove = false;
  }, "Throw and pull");

  InlineEffect PullEffect => new(s => {
    s.HasGravity = false;
    s.CanRotate = false;
    s.CanMove = false;
  }, "Throw and pull");

  InlineEffect VaultEffect => new(s => {
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.Times(.25f));
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
  }, "Vault effect");

  void FixedUpdate() {
    var layerMask = Defaults.Instance.GrapplePointLayerMask;
    var queryTriggerInteraction = QueryTriggerInteraction.Collide;
    var toTarget = float.MaxValue;
    var eye = transform.position;
    Candidate = null;
    foreach (var grapplePoint in GrapplePointManager.Instance.Points) {
      var isVisible = grapplePoint.transform.IsVisibleFrom(eye, layerMask, queryTriggerInteraction);
      var toGrapplePoint = Vector3.Distance(transform.position, grapplePoint.transform.position);
      if (isVisible && toGrapplePoint < toTarget) {
        Candidate = grapplePoint;
        toTarget = toGrapplePoint;
      }
    }
    Candidate?.Sources.Add(transform.position);
  }

  void LateUpdate() {
    GrappleLine.SetPosition(0, HookOrigin.Transform.position);
  }

  public override async Task MainAction(TaskScope scope) {
    try {
      if (Candidate != null) {
        Target = Candidate;
        using (var activeEffect = Status.Add(ActiveEffect)) {
          await scope.Any(
            Waiter.Repeat(UpdateCurrentRotationFromTarget),
            Waiter.Sequence(Windup, Throw));
        }
        using (var pullEffect = Status.Add(PullEffect)) {
          await scope.Any(
            Waiter.Repeat(UpdateCurrentRotationFromVelocity),
            Pull);
        }
        using (var vaultEffect = Status.Add(VaultEffect)) {
          await scope.Any(
            Waiter.Repeat(UpdateCurrentRotationFromVelocity),
            Vault);
        }
      } else {
        await scope.Tick();
      }
    } finally {
      Target = null;
      Velocity = Vector3.zero;
      CurrentRotation = 0;
      GrappleLine.enabled = false;
      Animator.SetInteger("GrappleState", 0);
      Animator.SetFloat("GrappleRotation", CurrentRotation);
    }
  }

  void UpdateCurrentRotationFromTarget() {
    var delta = Target.transform.position-transform.position;
    var toTarget = delta.normalized;
    var rotationDelta = Time.fixedDeltaTime * RotationSpeed;
    var desiredRotation = Vector3.Dot(toTarget, Vector3.up);
    CurrentRotation = Mathf.MoveTowards(CurrentRotation, desiredRotation, rotationDelta);
    Animator.SetFloat("GrappleRotation", CurrentRotation);
  }

  void UpdateCurrentRotationFromVelocity() {
    var rotationDelta = Time.fixedDeltaTime * RotationSpeed;
    var desiredRotation = Vector3.Dot(Velocity.normalized, Vector3.up);
    CurrentRotation = Mathf.MoveTowards(CurrentRotation, desiredRotation, rotationDelta);
    Animator.SetFloat("GrappleRotation", CurrentRotation);
  }

  async Task Windup(TaskScope scope) {
    Animator.SetInteger("GrappleState", 1);
    GrappleLine.enabled = false;
    for (var i = 0; i < WindupDuration.Ticks; i++) {
      var delta = Target.transform.position-transform.position;
      var toTargetXZ = delta.XZ().normalized;
      Mover.transform.rotation = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      await scope.Tick();
    }
  }

  async Task Throw(TaskScope scope) {
    Animator.SetInteger("GrappleState", 1);
    ClipSource.PlayOneShot(ThrowClip);
    GrappleLine.enabled = true;
    Destroy(Instantiate(ThrowVFX, HookOrigin.Transform.position, transform.rotation), 3);
    for (var i = 0; i <= ThrowDuration.Ticks; i++) {
      var origin = HookOrigin.Transform.position;
      var interpolant = (float)i/(float)ThrowDuration.Ticks;
      var destination = Target.transform.position;
      GrappleLine.SetPosition(1, Vector3.Lerp(origin, destination, interpolant));
      await scope.Tick();
    }
    ClipSource.PlayOneShot(HitClip);
    Destroy(Instantiate(HitVFX, Target.transform.position, transform.rotation), 3);
  }

  async Task Pull(TaskScope scope) {
    var origin = Mover.transform.position;
    var delta = Target.transform.position-Mover.transform.position;
    var distanceToTarget = delta.magnitude;
    var toTarget = delta.normalized;
    var toTargetXZ = delta.XZ().normalized;
    GrappleLine.enabled = true;
    Animator.SetInteger("GrappleState", 2);
    PullLoop.Play();
    var duration = Timeval.FromSeconds(distanceToTarget / PullSpeed);
    Velocity = delta / duration.Seconds;
    for (var i = 0; i <= duration.Ticks; i++) {
      GrappleLine.SetPosition(1, Target.transform.position);
      Mover.Move(Time.fixedDeltaTime * Velocity);
      await scope.Tick();
    }
    PullLoop.Stop();
  }

  async Task Vault(TaskScope scope) {
    GrappleLine.enabled = false;
    Animator.SetInteger("GrappleState", 3);
    ClipSource.PlayOneShot(VaultClip);
    Velocity *= VaultSpeedMultiplier;
    Destroy(Instantiate(VaultVFX, transform.position, transform.rotation), 3);
    while (!Status.IsGrounded) {
      Velocity -= Time.fixedDeltaTime * Attributes.GetValue(AttributeTag.Gravity, 0) * Vector3.up;
      Mover.Move(Time.fixedDeltaTime * Velocity);
      await scope.Tick();
    }
  }
}