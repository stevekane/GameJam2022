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
    if (Target) {
      var toTarget = transform.position.TryGetDirection(Target.transform.position) ?? transform.forward;
      Mover.SetAim(toTarget);
    }
  }

  async Task TryReposition(TaskScope scope) {
    var destination = Target.position+DesiredDistance*UnityEngine.Random.Range(0, 4) switch {
      0 => Target.forward,
      1 => -Target.forward,
      2 => Target.right,
      _ => -Target.right
    };
    var navMeshAreaIndex = NavMesh.GetAreaFromName(NavMeshAreaName);
    var navMeshMask = 1 << navMeshAreaIndex; // For real? wtf unity
    var validPosition = NavMesh.SamplePosition(destination, out var hit, NavMeshSearchRadius, navMeshMask);
    if (validPosition) {
      await scope.Repeat(RepositionDelay.Ticks, async s => {
        var toDestination = destination-transform.position;
        if (toDestination.magnitude > MinDistanceToDestination) {
          Mover.SetMove(toDestination.XZ().normalized);
        } else {
          Mover.SetMove(Vector3.zero);
        }
        await s.Tick();
      });
    } else {
      Mover.SetMove(Vector3.zero);
      await scope.Delay(RepositionDelay);
    }
  }

  async Task TryStartAbility(TaskScope scope) {
    var index = UnityEngine.Random.Range(0, Abilities.Length);
    var runningAbility = AbilityManager.TryRun(Abilities[index].MainAction);
    await scope.Tick();
    await scope.Tick();
    await scope.Run(runningAbility);
    await scope.Delay(AbilityDelay);
  }
}