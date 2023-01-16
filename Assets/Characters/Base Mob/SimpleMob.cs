using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(Mover))]
public class SimpleMob : MonoBehaviour {
  public enum RepositionBehaviors { ChaseTarget, RandomAroundTarget, RandomAroundMe };
  public enum TelegraphBehaviors { TelegraphThenAttack, TelegraphDuringAttack, DontTelegraph };
  [SerializeField] float RepositionDistance = 10f;
  [SerializeField] Timeval RepositionDelay = Timeval.FromSeconds(1);
  [SerializeField] RepositionBehaviors RepositionBehavior;
  [SerializeField] TelegraphBehaviors TelegraphBehavior;
  [SerializeField] float AttackRange = 10f;
  [SerializeField] Ability MainAttack;
  [SerializeField] Timeval AttackDelay = Timeval.FromSeconds(2);

  Transform Target;
  NavMeshAgent NavMeshAgent;
  AbilityManager AbilityManager;
  Status Status;
  Mover Mover;
  AIMover AIMover;
  Flash Flash;
  TaskScope MainScope = new();

  void Awake() {
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out AbilityManager);
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out Flash);
  }
  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryStartAbility));
  }

  void TryFindTarget() {
    Target = Target ? Target : Player.Get()?.transform;
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).normalized : transform.forward);
  }

  void TryMove() {
    AIMover.SetMoveFromNavMeshAgent();
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

  async Task TryStartAbility(TaskScope scope) {
    if (Status.IsGrounded && Status.CanAttack && TargetInRange(AttackRange)) {
      await scope.Any(
        Waiter.Until(() => Status.IsHurt),
        async s => {
          TaskFunc telegraph = TelegraphBehavior switch {
            TelegraphBehaviors.TelegraphThenAttack => async s => await Flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3),
            TelegraphBehaviors.TelegraphDuringAttack => async s => { _ = Flash.RunStrobe(s, Color.red, Timeval.FromMillis(150), 3); await scope.Yield(); },
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

  bool TargetInRange(float range) {
    var delta = (Target.position - transform.position);
    var dist = range;
    return delta.y < dist && delta.XZ().sqrMagnitude < dist*dist;
  }
  Vector3 ChaseTarget(float desiredDist) {
    var delta = Target.transform.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - NavMeshAgent.stoppingDistance) * delta.normalized;
  }
  Vector3 RandomAroundTarget(float desiredDist) {
    return Target.position + desiredDist * UnityEngine.Random.onUnitSphere.XZ();
  }
  Vector3 RandomAroundMe(float desiredDist) {
    return transform.position + desiredDist * UnityEngine.Random.onUnitSphere.XZ();
  }
}