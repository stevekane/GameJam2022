using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Badger : MonoBehaviour {
  public float AttackRange = 4f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Hurtbox Hurtbox;
  Transform Target;
  Transform Protectee;
  NavMeshAgent NavMeshAgent;
  Mover Mover;
  AbilityManager Abilities;
  AbilityManager TargetAbilities;
  Shield Shield;
  AttackAbility PunchAbility;
  ShieldAbility ShieldAbility;
  TaskScope MainScope = new();

  public static InlineEffect HurtStunEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "BadgerHurt");

  public void Awake() {
    Target = Player.Get().transform;
    TargetAbilities = Target.GetComponent<AbilityManager>();
    Status = GetComponent<Status>();
    Mover = GetComponent<Mover>();
    NavMeshAgent = GetComponent<NavMeshAgent>();
    Hurtbox = GetComponentInChildren<Hurtbox>();
    Shield = GetComponentInChildren<Shield>();
    Abilities = GetComponent<AbilityManager>();
    PunchAbility = GetComponentInChildren<AttackAbility>();
    ShieldAbility = GetComponentInChildren<ShieldAbility>();
    Hurtbox.OnHurt.Listen(async _ => {
      using var effect = Status.Add(HurtStunEffect);
      ShieldAbility.Stop();
      await MainScope.Millis(500);
    });
  }

  bool TargetIsAttacking => TargetAbilities.Abilities.Any(a => a.IsRunning && a.HitConfigData != null);
  bool TargetIsNear => (Target.position - transform.position).sqrMagnitude < 7f*7f;
  bool TargetInAttackRange => (Target.position - transform.position).sqrMagnitude < AttackRange*AttackRange;

  void ChooseProtectee() {
    if (Protectee)
      return;
    var protectees = FindObjectsOfType<Sniper>();
    if (protectees.Length == 0)
      return;
    Protectee = protectees[UnityEngine.Random.Range(0, protectees.Length-1)].transform;
  }
  // Try to get between target and protectee (or self, if no protectee).
  Vector3 ChoosePosition() {
    ChooseProtectee();
    var t = Target.transform;
    var other = Protectee ? Protectee.transform : transform;
    var dir = other.position - t.position;
    var d = AttackRange - NavMeshAgent.stoppingDistance;
    var pos = t.position + d*dir.normalized;
    return pos;
  }

  void Start() => MainScope.Start(Behavior);
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    await scope.All(
      Waiter.Repeat(TryFindTarget),
      Waiter.Repeat(TryAim),
      Waiter.Repeat(TryMove),
      Waiter.Repeat(TryReposition),
      Waiter.Repeat(TryAttack));
  }

  void TryFindTarget() {
    Target = Target ? Target : Player.Get()?.transform;
  }

  void TryAim() {
    Mover.SetAim(Target ? (Target.position-transform.position).XZ().normalized : transform.forward);
  }

  bool ShouldMove = true;
  void TryMove() {
    if (ShouldMove) {
      Mover.SetMoveFromNavMeshAgent();
    } else {
      Mover.SetMove(Vector3.zero);
    }
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(ChoosePosition());
    await scope.Tick();
  }

  async Task TryAttack(TaskScope scope) {
    if (Target && Status.CanAttack) {
      if (Shield && TargetIsAttacking && TargetIsNear) {
        ShouldMove = false;
        Abilities.TryInvoke(ShieldAbility.MainAction);
        // Hold shield until .5s after target stops attacking, or it dies.
        await scope.Any(
          TrueForDuration(Timeval.FromSeconds(.5f), () => !TargetIsAttacking),
          Waiter.While(() => Shield != null));
        await Abilities.TryRun(scope, ShieldAbility.MainRelease);
        ShouldMove = true;
      } else if (TargetInAttackRange) {
        //ShouldMove = false;
        Abilities.TryInvoke(PunchAbility.MainAction);
        await scope.Millis(750);
        Abilities.TryInvoke(PunchAbility.MainRelease);
        await scope.While(() => PunchAbility.IsRunning);
        await scope.Delay(AttackDelay);
        ShouldMove = true;
      }
    }
  }

  TaskFunc TrueForDuration(Timeval waitTime, Func<bool> pred) => async (TaskScope scope) => {
    var ticks = waitTime.Ticks;
    while (ticks-- > 0) {
      if (!pred())
        ticks = waitTime.Ticks;
      await scope.Tick();
    }
  };
}
