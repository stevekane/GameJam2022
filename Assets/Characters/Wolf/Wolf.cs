using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static UnityEditor.PlayerSettings;

public class Wolf : MonoBehaviour {
  public float DashRange = 8f;
  public float AttackRange = 3f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Hurtbox Hurtbox;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Mover Mover;
  AbilityManager Abilities;
  CycleAbility LightAbility;
  AttackAbility HeavyAbility;
  SwipeDash DashAbility;
  TaskScope MainScope = new();
  float DesiredDistance;

  public static InlineEffect HurtStunEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "WolfHurt");

  public void Awake() {
    Target = Player.Get().transform;
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out NavMeshAgent);
    this.InitComponentFromChildren(out Hurtbox);
    this.InitComponent(out Abilities);
    this.InitComponentFromChildren(out LightAbility);
    this.InitComponentFromChildren(out HeavyAbility);
    this.InitComponentFromChildren(out DashAbility);
    Hurtbox.OnHurt.Listen(async _ => {
      using var effect = Status.Add(HurtStunEffect);
      await MainScope.Millis(500);
    });
  }

  Vector3 ChoosePosition(float desiredDist) {
    var t = Target.transform;
    var delta = t.position.XZ() - transform.position.XZ();
    return transform.position + delta - desiredDist * delta.normalized;
  }
  bool IsInRange(Vector3 pos) {
    var delta = (pos - transform.position).XZ();
    return delta.sqrMagnitude < .1f;
  }
  bool TargetInRange() {
    var delta = (Target.position - transform.position);
    var dist = AttackRange;
    return delta.y < dist && delta.XZ().sqrMagnitude < dist*dist;
  }

  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    DesiredDistance = DashRange;
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
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
      Mover.SetMoveFromNavMeshAgent();
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(ChoosePosition(DesiredDistance));
    await scope.Tick();
  }

  async Task TryAttackSequence(TaskScope scope) {
    if (Target && Status.CanAttack && IsInRange(NavMeshAgent.destination)) {
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
          Waiter.Until(() => Status.IsHurt));
      }
      ShouldMove = false;
      await scope.Delay(AttackDelay);
      ShouldMove = true;
      DesiredDistance = DashRange;
    }
  }

  async Task TryFinisherSequence(TaskScope scope) {
    await scope.Until(() => TargetInRange() && Status.CanAttack);
    await Abilities.TryRun(scope, LightAbility.MainAction);
    await scope.Until(() => TargetInRange() && Status.CanAttack);
    await Abilities.TryRun(scope, LightAbility.MainAction);
    await scope.Until(() => TargetInRange() && Status.CanAttack);
    await Abilities.TryRun(scope, HeavyAbility.MainAction);
  }
}
