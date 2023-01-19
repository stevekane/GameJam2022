using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SmokeWitch : MonoBehaviour {
  public float DefensiveRange = 8f;
  public Timeval AttackCooldown = Timeval.FromMillis(1000);

  AttackSequence[] AttackSequences;
  Status Status;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Mover Mover;
  AIMover AIMover;
  AbilityManager AbilityManager;
  SimpleDash DashAbility;
  Jump JumpAbility;
  TaskScope MainScope = new();
  float DesiredDistance => CurrentAttack?.DesiredDistance ?? DefensiveRange;
  Vector3? DesiredPosition = null;

  public void Awake() {
    Target = Player.Get().transform;
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out AbilityManager);
    this.InitComponentFromChildren(out DashAbility);
    this.InitComponentFromChildren(out JumpAbility);
    AttackSequences = GetComponentsInChildren<AttackSequence>();
  }

  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
      Waiter.Repeat(TryRecover),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryAttackSequence));
  }

  void TryFindTarget() {
    Target = Target ? Target : Player.Get()?.transform;
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).XZ().normalized : transform.forward);
  }

  bool ShouldMove = true;  // TODO?
  void TryMove() {
    if (ShouldMove)
      AIMover.SetMoveFromNavMeshAgent();
  }

  async Task TryRecover(TaskScope scope) {
    bool OverVoid() {
      const float MAX_RAYCAST_DISTANCE = 1000;
      return !Physics.Raycast(transform.position, Vector3.down, MAX_RAYCAST_DISTANCE, Defaults.Instance.EnvironmentLayerMask);
    }
    Vector3 FindRecoverPosition() {
      var edge = NavMeshUtil.Instance.FindClosestPointOnEdge(transform.position);
      var delta = edge - transform.position;
      return transform.position + delta + 2f*delta.XZ().normalized;
    }
    if (ShouldMove && Status.CanAttack && !Status.IsGrounded && OverVoid()) {
      DesiredPosition = FindRecoverPosition();
      await AbilityManager.TryRun(scope, DashAbility.MainAction);

      DesiredPosition = FindRecoverPosition();
      await AbilityManager.TryRun(scope, JumpAbility.MainAction);

      await scope.Until(() => Status.IsGrounded);
      DesiredPosition = null;
    }
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(ChaseTarget(DesiredDistance));
    await scope.Tick();
  }

  AttackSequence CurrentAttack;
  async Task TryAttackSequence(TaskScope scope) {
    if (Target && Status.CanAttack && ChooseAttackSequence() is var seq && seq) {
      CurrentAttack = seq;
      await seq.Perform(scope, Target);
      CurrentAttack = null;
      await scope.Delay(AttackCooldown);
    }
  }

  // Choose one of the startable sequences using a weighted random pick.
  AttackSequence ChooseAttackSequence() {
    var usableSequences = AttackSequences.Where(seq => seq.CanStart(Target));
    var totalScore = usableSequences.Sum(seq => seq.Score);
    var chosenScore = UnityEngine.Random.Range(0f, totalScore);
    var score = 0f;
    foreach (var seq in usableSequences) {
      score += seq.Score;
      if (chosenScore <= score)
        return seq;
    }
    return null;
  }

  Vector3 ChaseTarget(float desiredDist) {
    if (DesiredPosition.HasValue)
      return DesiredPosition.Value;
    var delta = Target.transform.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - NavMeshAgent.stoppingDistance) * delta.normalized;
  }

  void OnDrawGizmos() {
    Gizmos.color = Color.magenta;
    if (DesiredPosition.HasValue)
      Gizmos.DrawWireSphere(DesiredPosition.Value, .5f);
  }
}
