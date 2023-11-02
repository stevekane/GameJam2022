using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

public class SimpleDash : Ability {
  public float MaxMoveSpeed = 120f;
  public float MinMoveSpeed = 60f;
  public float TurnSpeed = 60f;
  public Timeval DashDuration = Timeval.FromSeconds(.3f);
  public Timeval ResidualImagePeriod = Timeval.FromMillis(50);
  public AnimationJobConfig Animation;
  public AudioClip LaunchSFX;
  public GameObject LaunchVFX;
  public Vector3 VFXOffset;
  public VisualEffect VisualEffect;

  public static InlineEffect ScriptedMove => new(s => {
    s.HasGravity = false;
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
  }, "DashMove");
  public static InlineEffect Invulnerable => new(s => {
    s.IsDamageable = false;
    s.IsHittable = false;
  }, "DashInvulnerable");

  public override bool CanStart(AbilityMethod func) =>
    func == MainRelease ? true :
    AbilityManager.GetAxis(AxisTag.ReallyAim).XZ == Vector3.zero &&
    (Status.IsGrounded || AirDashRemaining > 0);

  // Button press/release.
  int AirDashRemaining = 1;
  public override async Task MainAction(TaskScope scope) {
    try {
      var dir = AbilityManager.GetAxis(AxisTag.Move).XZ.TryGetDirection() ?? AbilityManager.transform.forward;
      using var moveEffect = Status.Add(ScriptedMove);
      using var invulnEffect = Status.Add(Invulnerable);
      SFXManager.Instance.TryPlayOneShot(LaunchSFX);
      VFXManager.Instance.TrySpawnEffect(LaunchVFX, transform.position + VFXOffset, transform.rotation);
      VisualEffect.Play();
      AnimationDriver.Play(scope, Animation);
      AirDashRemaining--;
      await scope.Any(
        Waiter.Delay(DashDuration),
        Waiter.Repeat(SpawnResidualImage),
        Waiter.Repeat(Move(dir.normalized)),
        MakeCancellable);
    } finally {
      VisualEffect.Stop();
    }
  }

  TaskFunc Move(Vector3 dir) => async (TaskScope scope) => {
    var desiredDir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    var desiredSpeed = Mathf.SmoothStep(MinMoveSpeed, MaxMoveSpeed, desiredDir.magnitude);
    var targetDir = desiredDir.TryGetDirection() ?? dir;
    dir = Vector3.RotateTowards(dir, targetDir.normalized, TurnSpeed/360f, 0f);
    Status.transform.forward = dir;
    Mover.Move(desiredSpeed * Time.fixedDeltaTime * dir);
    await scope.Tick();
  };

  async Task SpawnResidualImage(TaskScope scope) {
    if (Status.TryGetComponent(out ResidualImageRenderer renderer)) {
      renderer.RenderImage();
    }
    await scope.Delay(ResidualImagePeriod);
  }

  async Task MakeCancellable(TaskScope scope) {
    await scope.Millis((int)(DashDuration.Millis / 3));
    Tags.AddFlags(AbilityTag.Cancellable);
    await scope.Forever();
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    if (Status.IsGrounded)
      AirDashRemaining = 1;
  }
}