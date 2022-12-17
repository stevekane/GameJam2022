using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/*
Soul Warrior always wants to close distance on the player
but also does not want to be too close. It understands the danger
of being in the player's primary attack range (5 units) and will
prefer to hop backwards or teleport if it finds itself there.

It looks for openings in the player's attack pattern while moving
around to try to stay in its preferred striking range.

Soul Warrior Attacks:

- Teleport above player into dive (strong knockback)
- Charging slash (medium lateral knockback)
- Parry will listen for an attack, block the damage from that attack, and fire a very large very fast counter attack
- Homing fireball. Spawns a fireball that homes on the player until it hits them or the environment
- Spawn Minion. Spawns a weak minion that just tries to move at the player for now.

Soul Warrior Mobility

- Hop backwards used when too close to the player
*/
public class SoulWarriorBehavior : MonoBehaviour {
  [SerializeField] Teleport Teleport;
  [SerializeField] BackDash BackDash;
  [SerializeField] Dive Dive;
  [SerializeField] float DangerDistance = 5f;
  [SerializeField] float DiveHeight = 15f;
  [SerializeField] TargetingConfig TargetingConfig;

  Mover Mover;
  Status Status;
  AbilityManager AbilityManager;
  Transform Target;
  TaskScope MainScope = new();

  void Awake() {
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    if (!Target) {
      Target = FindObjectOfType<Player>().transform;
      await scope.Tick();
    } else {
      var delta = (Target.position-transform.position).XZ();
      var toPlayer = delta.normalized;
      var distanceToPlayer = delta.magnitude;
      Mover.SetAim(toPlayer);
      Mover.SetMove(toPlayer);
      if (Status.IsGrounded) {
        if (distanceToPlayer < DangerDistance) {
          Mover.SetMove(Vector3.zero);
          await AbilityManager.TryRun(scope, BackDash.MainAction);
          await scope.Tick();
        } else {
          var diceRoll = Random.Range(0f, 1f);
          if (diceRoll < .01f) {
            Mover.SetMove(Vector3.zero);
            Teleport.Destination = transform.position + delta + DiveHeight * Vector3.up;
            await AbilityManager.TryRun(scope, Teleport.MainAction);
            await scope.Tick();
          } else {
            await scope.Tick();
          }
        }
      } else {
        if (transform.position.y > Target.position.y) {
          Mover.SetMove(Vector3.zero);
          await AbilityManager.TryRun(scope, Dive.MainAction);
        }
        await scope.Tick();
      }
    }
  }
}