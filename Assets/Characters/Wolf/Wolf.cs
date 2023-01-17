using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Wolf : MonoBehaviour {
  public float DashRange = 8f;
  public float AttackRange = 3f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Hurtbox Hurtbox;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Flash Flash;
  Mover Mover;
  AIMover AIMover;
  AbilityManager Abilities;
  CycleAbility LightAbility;
  AttackAbility HeavyAbility;
  SwipeDash DashAbility;
  Jump JumpAbility;
  TaskScope MainScope = new();
  float DesiredDistance;
  Vector3? DesiredPosition = null;

  public static InlineEffect HurtStunEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "WolfHurt");

  public void Awake() {
    Target = Player.Get().transform;
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out Flash);
    this.InitComponentFromChildren(out Hurtbox);
    this.InitComponent(out Abilities);
    this.InitComponentFromChildren(out LightAbility);
    this.InitComponentFromChildren(out HeavyAbility);
    this.InitComponentFromChildren(out DashAbility);
    this.InitComponentFromChildren(out JumpAbility);
    Hurtbox.OnHurt.Listen(async _ => {
      using var effect = Status.Add(HurtStunEffect);
      await MainScope.Millis(500);
    });
  }

  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  Vector3 ChoosePosition(float desiredDist) {
    if (DesiredPosition.HasValue)
      return DesiredPosition.Value;
    var t = Target.transform;
    var delta = t.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - NavMeshAgent.stoppingDistance) * delta.normalized;
  }
  bool TargetInXZRange(float range) {
    var delta = (Target.position - transform.position);
    var dist = range;
    return delta.XZ().sqrMagnitude < dist*dist;
  }

  bool TargetInRange(float range) {
    var delta = (Target.position - transform.position);
    var dist = range;
    return delta.y < dist && delta.XZ().sqrMagnitude < dist*dist;
  }

  async Task Behavior(TaskScope scope) {
    DesiredDistance = DashRange;
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
      Waiter.Repeat(TryJump),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryAttackSequence));
  }

  void TryFindTarget() {
    Target = Target ? Target : Player.Get()?.transform;
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).XZ().normalized : transform.forward);
  }

  bool ShouldMove = true;
  void TryMove() {
    if (ShouldMove)
      AIMover.SetMoveFromNavMeshAgent();
  }

  async Task TryJump(TaskScope scope) {
    bool OverVoid() {
      const float MAX_RAYCAST_DISTANCE = 1000;
      return !Physics.Raycast(transform.position, Vector3.down, MAX_RAYCAST_DISTANCE, Defaults.Instance.EnvironmentLayerMask);
    }
    if (ShouldMove && Status.CanAttack && !Status.IsGrounded && OverVoid()) {
      DesiredDistance = 0f;
      DashAbility.Target = Target;
      await Abilities.TryRun(scope, DashAbility.MainAction);
      // TODO: This sucks - it will only find a point on his old surface. There doesn't seem to be a way to find the nearest surface?
      if (NavMeshAgent.FindClosestEdge(out var hit)) {
        var delta = hit.position - transform.position;
        DesiredPosition = transform.position + delta + 2f*delta.XZ().normalized;
        Debug.DrawRay(hit.position, Vector3.up*4f);
      }
      await Abilities.TryRun(scope, JumpAbility.MainAction);
      await scope.Until(() => Status.IsGrounded);
      DesiredPosition = null;
    }
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(ChoosePosition(DesiredDistance));
    await scope.Tick();
  }

  async Task TryAttackSequence(TaskScope scope) {
    if (Target && Status.CanAttack && TargetInRange(DashRange)) {
      await Flash.RunStrobe(scope, Color.red, Timeval.FromMillis(150), 3);
      var didHit = false;
      DashAbility.Target = Target;
      DashAbility.OnHit = _ => didHit = true;  // TODO: check if the player was the collider?
      ShouldMove = false;
      await Abilities.TryRun(scope, DashAbility.MainAction);
      ShouldMove = true;
      if (didHit) {
        await scope.Millis(200);
        DesiredDistance = AttackRange*.7f;
        await scope.Any(
          TryFinisherSequence,
          Waiter.Until(() => !TargetInXZRange(DashRange)),
          Waiter.Until(() => Status.IsHurt));
      }
      ShouldMove = false;
      await scope.Delay(AttackDelay);
      ShouldMove = true;
      DesiredDistance = DashRange;
    }
  }

  async Task TryFinisherSequence(TaskScope scope) {
    await scope.Until(() => TargetInRange(AttackRange) && Status.CanAttack);
    await Abilities.TryRun(scope, LightAbility.MainAction);
    await scope.Until(() => TargetInRange(AttackRange) && Status.CanAttack);
    await Abilities.TryRun(scope, LightAbility.MainAction);
    await scope.Until(() => TargetInRange(AttackRange) && Status.CanAttack);
    await Abilities.TryRun(scope, HeavyAbility.MainAction);
  }
}
