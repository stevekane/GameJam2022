using System.Threading.Tasks;
using UnityEngine;

public class Grapple : Ability {
  const int NotGrappling = 0;
  const int Throwing = 1;
  const int Pulling = 2;
  const int Vaulting = 3;

  [SerializeField] AvatarTransform HookOrigin;
  [SerializeField] LineRenderer GrappleAimLine;
  [SerializeField] LineRenderer GrappleLine;
  [SerializeField] Animator Animator;
  [SerializeField] float AimLocalTimeDilation = 1;
  [SerializeField] float PullSpeed = 15;
  [SerializeField] float RotationSpeed = 180;
  [SerializeField] float VaultSpeedMultiplier = 1.25f;
  [SerializeField] float VaultDrag = 2;
  [SerializeField] Timeval WindupDuration = Timeval.FromMillis(250);
  [SerializeField] Timeval ThrowDuration = Timeval.FromMillis(100);
  [SerializeField] Timeval HangDuration = Timeval.FromMillis(300);
  [SerializeField] Timeval HoldDuration = Timeval.FromMillis(100);
  [SerializeField] Timeval VaultDuration = Timeval.FromSeconds(1);
  [SerializeField] AudioSource PullLoop;
  [SerializeField] AudioSource ClipSource;
  [SerializeField] AudioClip ThrowClip;
  [SerializeField] AudioClip HitClip;
  [SerializeField] AudioClip VaultClip;
  [SerializeField] GameObject ThrowVFX;
  [SerializeField] GameObject HitVFX;
  [SerializeField] GameObject VaultVFX;

  public GrapplePoint ScriptedTarget { get; set; }
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

  protected override void FixedUpdate() {
    base.FixedUpdate();
    var aim = AbilityManager.GetAxis(AxisTag.ReallyAim);
    var aiming = aim.XZ.sqrMagnitude > 0;
    var direction = aiming ? aim.XZ : transform.forward.XZ();
    var bestScore = float.MaxValue;
    var eye = transform.position;
    Candidate = null;
    if (aiming) {
      foreach (var grapplePoint in GrapplePointManager.Instance.Points) {
        var isVisible = grapplePoint.transform.IsVisibleFrom(eye, Defaults.Instance.GrapplePointLayerMask, QueryTriggerInteraction.Collide);
        var dist = Vector3.Distance(transform.position, grapplePoint.transform.position);
        var angle = Mathf.Abs(Vector3.Angle(direction, (grapplePoint.transform.position - eye).XZ()));
        var score = angle > 180f ? float.MaxValue : 100f*(angle/180f) + dist;
        if (isVisible && score < bestScore) {
          Candidate = grapplePoint;
          bestScore = score;
        }
      }
      if (Candidate != null) {
        Status.AddNextTick(s => s.AddAttributeModifier(AttributeTag.LocalTimeScale, AttributeModifier.Times(AimLocalTimeDilation)));
        GrappleAimLine.SetPosition(1, Candidate.transform.position);
      }
    }
    GrappleAimLine.enabled = aiming;
  }

  void LateUpdate() {
    GrappleAimLine.SetPosition(0, HookOrigin.Transform.position);
    GrappleLine.SetPosition(0, HookOrigin.Transform.position);
  }

  public override bool CanStart(AbilityMethod func) => Candidate ?? ScriptedTarget;

  public override async Task MainAction(TaskScope scope) {
    try {
      Target = Candidate ?? ScriptedTarget;
      using (var activeEffect = Status.Add(ActiveEffect)) {
        await scope.Any(
          Waiter.Repeat(UpdateCurrentRotationFromTarget),
          Waiter.Sequence(Windup, Throw, Waiter.Delay(HangDuration)));
      }
      using (var pullEffect = Status.Add(PullEffect)) {
        await scope.Any(
          Waiter.Repeat(UpdateCurrentRotationFromVelocity),
          Waiter.Sequence(Pull, Waiter.Delay(HoldDuration)));
      }
      await scope.Run(Vault);
    } finally {
      Target = null;
      CurrentRotation = 0;
      GrappleLine.enabled = false;
      Animator.SetInteger("GrappleState", NotGrappling);
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
    var rotationDelta = Time.fixedDeltaTime * RotationSpeed / 90; // degrees/s to (-1)-1 unts/s
    var desiredRotation = Vector3.Dot(Velocity.normalized, Vector3.up);
    CurrentRotation = Mathf.MoveTowards(CurrentRotation, desiredRotation, rotationDelta);
    Animator.SetFloat("GrappleRotation", CurrentRotation);
  }

  async Task Windup(TaskScope scope) {
    Animator.SetInteger("GrappleState", Throwing);
    GrappleLine.enabled = false;
    for (var i = 0; i < WindupDuration.Ticks; i++) {
      var delta = Target.transform.position-transform.position;
      var toTargetXZ = delta.XZ().normalized;
      Mover.transform.rotation = Quaternion.LookRotation(toTargetXZ, Vector3.up);
      await scope.Tick();
    }
  }

  async Task Throw(TaskScope scope) {
    Animator.SetInteger("GrappleState", Throwing);
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
    Animator.SetInteger("GrappleState", Pulling);
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
    ClipSource.PlayOneShot(VaultClip);
    Velocity *= VaultSpeedMultiplier;
    Destroy(Instantiate(VaultVFX, transform.position, transform.rotation), 3);
    Status.Add(new VaultEffect(Velocity, VaultDrag));
    await scope.Tick();
  }
}