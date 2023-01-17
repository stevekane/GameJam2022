using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(Mover))]
public class SimpleMob : MonoBehaviour {
  public enum RepositionBehaviors { ChaseTarget, RandomAroundTarget, RandomAroundMe };
  public enum TelegraphBehaviors { TelegraphThenAttack, TelegraphDuringAttack, DontTelegraph };
  [SerializeField] float RepositionDistance = 4f;
  [SerializeField] Timeval RepositionDelay = Timeval.FromSeconds(1);
  [SerializeField] RepositionBehaviors RepositionBehavior;
  [SerializeField] TelegraphBehaviors TelegraphBehavior;
  [SerializeField] float BlockRange = 10f;
  [SerializeField] float AttackRange = 4f;
  [SerializeField] Ability MainAttack;
  [SerializeField] Timeval AttackDelay = Timeval.FromSeconds(2);

  Transform Target;
  AbilityManager TargetAbilities;
  NavMeshAgent NavMeshAgent;
  AbilityManager AbilityManager;
  Status Status;
  Mover Mover;
  AIMover AIMover;
  Flash Flash;
  Shield Shield;
  ShieldAbility ShieldAbility;
  TaskScope MainScope = new();

  void Awake() {
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out AbilityManager);
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out Flash);
    this.InitComponentFromChildren(out Shield, optional: true);
    this.InitComponentFromChildren(out ShieldAbility, optional: true);
  }
  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryBlock),
      Waiter.Repeat(TryAttack));
  }

  void TryFindTarget() {
    if (!Target) {
      Target = Player.Get()?.transform;
      TargetAbilities = Target.GetComponent<AbilityManager>();
    }
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).normalized : transform.forward);
  }

  bool ShouldMove = true;
  async Task TryMove(TaskScope scope) {
    if (ShouldMove) {
      AIMover.SetMoveFromNavMeshAgent();
    } else {
      Mover.SetMove(Vector3.zero);
    }
    await scope.Tick();
  }

  async Task TryReposition(TaskScope scope) {
    var pos = RepositionBehavior switch {
      RepositionBehaviors.ChaseTarget => ChaseTarget(RepositionDistance),
      RepositionBehaviors.RandomAroundTarget => RandomAroundTarget(RepositionDistance),
      RepositionBehaviors.RandomAroundMe => RandomAroundMe(RepositionDistance),
      _ => Vector3.zero,
    };
    NavMeshAgent.SetDestination(pos);
    await scope.Delay(RepositionDelay);
  }

  async Task TryAttack(TaskScope scope) {
    if (Status.IsGrounded && Status.CanAttack && !IsBlocking && TargetInRange(AttackRange)) {
      await scope.Any(
        Waiter.Until(() => Status.IsHurt),
        async s => {
          TaskFunc telegraph = TelegraphBehavior switch {
            TelegraphBehaviors.TelegraphThenAttack => async s => await Flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3),
            TelegraphBehaviors.TelegraphDuringAttack => async s => { _ = Flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3); await scope.Yield(); }
            ,
            TelegraphBehaviors.DontTelegraph => async s => await s.Yield(),
            _ => null,
          };
          await telegraph(s);
          if (MainAttack is Throw t)
            t.Target = Target;
          await AbilityManager.TryRun(s, MainAttack.MainAction);
        });
      await scope.Delay(AttackDelay);
    }
  }

  async Task TryBlock(TaskScope scope) {
    if (!Shield)
      await scope.Forever();
    if (Status.IsGrounded && Status.CanAttack && !IsAttacking && TargetIsAttacking && TargetInRange(BlockRange)) {
      ShouldMove = false;
      AbilityManager.TryInvoke(ShieldAbility.MainAction);
      // Hold shield until .5s after target stops attacking, or shield dies.
      await scope.Any(
        TrueForDuration(Timeval.FromSeconds(.5f), () => !TargetIsAttacking),
        Waiter.While(() => Shield != null));
      await AbilityManager.TryRun(scope, ShieldAbility.MainRelease);
      ShouldMove = true;

      TaskFunc TrueForDuration(Timeval waitTime, Func<bool> pred) => async (TaskScope scope) => {
        var ticks = waitTime.Ticks;
        while (ticks-- > 0) {
          if (!pred())
            ticks = waitTime.Ticks;
          await scope.Tick();
        }
      };
    }
  }

  bool IsAttacking => MainAttack.IsRunning;
  bool IsBlocking => ShieldAbility && ShieldAbility.IsRunning;
  bool TargetIsAttacking => TargetAbilities.Abilities.Any(a => a.IsRunning && a.HitConfigData != null);
  bool TargetInRange(float range) {
    var delta = (Target.position - transform.position);
    return delta.y < range && delta.XZ().sqrMagnitude < range*range;
  }
  Vector3 ChaseTarget(float desiredDist) {
    var delta = Target.transform.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - NavMeshAgent.stoppingDistance) * delta.normalized;
  }
  Vector3 RandomAroundTarget(float desiredDist) {
    return Target.position + desiredDist * UnityEngine.Random.insideUnitCircle.normalized.XZ();
  }
  Vector3 RandomAroundMe(float desiredDist) {
    return transform.position + desiredDist * UnityEngine.Random.insideUnitCircle.normalized.XZ();
  }
}