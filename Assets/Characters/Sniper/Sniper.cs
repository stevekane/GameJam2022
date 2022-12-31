using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Sniper : MonoBehaviour {
  [SerializeField] string NavMeshAreaName = "Walkable";
  [SerializeField] float NavMeshSearchRadius = 1f;
  [SerializeField] float DesiredDistance = 10f;
  [SerializeField] float MinDistanceToDestination = 1f;
  [SerializeField] Timeval RepositionDelay = Timeval.FromSeconds(1);
  [SerializeField] Timeval AbilityDelay = Timeval.FromSeconds(2);

  Transform Target;
  Mover Mover;
  AbilityManager AbilityManager;
  Ability[] Abilities;
  TaskScope Scope = new();

  void Awake() {
    Mover = GetComponent<Mover>();
    AbilityManager = GetComponent<AbilityManager>();
    Abilities = GetComponentsInChildren<Ability>();
  }
  void Start() => Scope.Start(Behavior);
  void OnDestroy() => Scope.Dispose();

  async Task Behavior(TaskScope scope) {
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryStartAbility));
  }

  void TryFindTarget() {
    Target = Target ? Target : FindObjectOfType<Player>()?.transform;
  }

  void TryAim() {
    var aim = Target ? (Target.position-transform.position).normalized : transform.forward;
    Mover.SetAim(aim);
  }

  async Task TryReposition(TaskScope scope) {
    var destination = Target.position+Random.onUnitSphere.XZ()*DesiredDistance;
    if (ValidNavMeshPosition(destination, NavMeshAreaName)) {
      await scope.Repeat(RepositionDelay.Ticks, async s => {
        var toDestination = destination-transform.position;
        Mover.SetMove(toDestination.magnitude > MinDistanceToDestination ? toDestination.normalized : Vector3.zero);
        await s.Tick();
      });
    } else {
      Mover.SetMove(Vector3.zero);
      await scope.Delay(RepositionDelay);
    }
  }

  async Task TryStartAbility(TaskScope scope) {
    var index = UnityEngine.Random.Range(0, Abilities.Length);
    await scope.Run(RunAbility(Abilities[index]));
    await scope.Delay(AbilityDelay);
  }

  TaskFunc RunAbility(Ability ability) => async scope => {
    var runningAbility = AbilityManager.TryRun(ability.MainAction);
    await scope.Tick();
    await scope.Tick();
    await scope.Run(runningAbility);
  };

  bool ValidNavMeshPosition(Vector3 position, string areaName = "Walkable") {
    var navMeshAreaIndex = NavMesh.GetAreaFromName(NavMeshAreaName);
    var navMeshMask = 1 << navMeshAreaIndex; // For real? wtf unity
    return NavMesh.SamplePosition(position, out var hit, NavMeshSearchRadius, navMeshMask);
  }
}