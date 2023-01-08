using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class Badger : MonoBehaviour {
  public float AttackRange = 2f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Hurtbox Hurtbox;
  Transform Target;
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

  Vector3 ChoosePosition() {
    var t = Target.transform;
    var d = AttackRange*.9f;
    var choices = new[] { t.position + t.right*d, t.position - t.right*d, t.position - t.forward*d };
    var closest = choices[0];
    foreach (var p in choices) {
      if ((p - transform.position).XZ().sqrMagnitude < (p - closest).XZ().sqrMagnitude)
        closest = p;
    }
    return closest;
  }
  bool IsInRange(Vector3 pos) {
    var delta = (pos - transform.position).XZ();
    return delta.sqrMagnitude < .1f;
  }

  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    if (Target == null) {
      MainScope.Cancel();
      return;
    }

    var desiredPos = ChoosePosition();
    var desiredFacing = (Target.position - transform.position).XZ().normalized;
    var inRange = IsInRange(desiredPos);

    async Task MaybeAttack(TaskScope scope) {
      if (Status.CanAttack) {
        if (Shield && TargetIsAttacking && TargetIsNear) {
          Abilities.TryInvoke(ShieldAbility.MainAction);
          // Hold shield until .5s after target stops attacking, or it dies.
          await scope.Any(
            TrueForDuration(Timeval.FromSeconds(.5f), () => !TargetIsAttacking),
            Waiter.While(() => Shield != null));
          await Abilities.TryRun(scope, ShieldAbility.MainRelease);
        } else if (inRange) {
          await Abilities.TryRun(scope, PunchAbility.MainAction);
          await scope.Delay(AttackDelay);
        }
      }
    }

    async Task Move(TaskScope scope) {
      NavMeshAgent.SetDestination(desiredPos);
      Mover.SetMoveFromNavMeshAgent();
      await scope.Tick();
    }

    Mover.SetMoveAim(Vector3.zero, desiredFacing);
    await MaybeAttack(scope);
    await Move(scope);
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
