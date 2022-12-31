using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(Mover))]
public class Sniper : MonoBehaviour {
  [SerializeField] string NavMeshAreaName = "Walkable";
  [SerializeField] float NavMeshSearchRadius = 1f;
  [SerializeField] float DesiredDistance = 10f;
  [SerializeField] Timeval RepositionDelay = Timeval.FromSeconds(1);
  [SerializeField] Timeval AbilityDelay = Timeval.FromSeconds(2);

  Transform Target;
  NavMeshAgent NavMeshAgent;
  AbilityManager AbilityManager;
  Mover Mover;
  Ability[] Abilities;
  TaskScope Scope = new();

  void Awake() {
    NavMeshAgent = GetComponent<NavMeshAgent>();
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
    AbilityManager = GetComponent<AbilityManager>();
    Mover = GetComponent<Mover>();
    Abilities = GetComponentsInChildren<Ability>();
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
    Target = Target ? Target : FindObjectOfType<Player>()?.transform;
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).normalized : transform.forward);
  }

  void TryMove() {
    NavMeshAgent.nextPosition = transform.position;
    Mover.SetMove(NavMeshAgent.desiredVelocity.normalized);
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(Target.position+Random.onUnitSphere.XZ()*DesiredDistance);
    await scope.Delay(RepositionDelay);
  }

  async Task TryStartAbility(TaskScope scope) {
    var index = UnityEngine.Random.Range(0, Abilities.Length);
    await scope.Run(Abilities[index].MainAction);
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