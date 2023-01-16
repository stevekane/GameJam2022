using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(Mover))]
public class Sniper : MonoBehaviour {
  [SerializeField] float DesiredDistance = 10f;
  [SerializeField] Timeval RepositionDelay = Timeval.FromSeconds(1);
  [SerializeField] Timeval AbilityDelay = Timeval.FromSeconds(2);

  Transform Target;
  NavMeshAgent NavMeshAgent;
  AbilityManager AbilityManager;
  Mover Mover;
  AIMover AIMover;
  Flash Flash;
  Throw Throw;
  TaskScope Scope = new();

  void Awake() {
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out AbilityManager);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out Flash);
    this.InitComponentFromChildren(out Throw);
  }
  void Start() => Scope.Start(Behavior);
  void OnDestroy() => Scope.Dispose();

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
    NavMeshAgent.SetDestination(Target.position+Random.onUnitSphere.XZ()*DesiredDistance);
    await scope.Delay(RepositionDelay);
  }

  async Task TryStartAbility(TaskScope scope) {
    await Flash.RunStrobe(scope, Color.red, Timeval.FromMillis(150), 3);
    Throw.Target = Target;
    await AbilityManager.TryRun(scope, Throw.MainAction);
    await scope.Delay(AbilityDelay);
  }

  void OnDrawGizmos() {
    if (NavMeshAgent) {
      var corners = NavMeshAgent.path.corners;
      for (var i = 0; i < corners.Length-1; i++) {
        Gizmos.DrawLine(Vector3.up+corners[i], Vector3.up+corners[i+1]);
      }
    }
  }
}