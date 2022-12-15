using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Badger : MonoBehaviour {
  public float AttackRange = 2f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Defender Defender;
  Hurtbox Hurtbox;
  Transform Target;
  AbilityManager Abilities;
  AbilityManager TargetAbilities;
  Shield Shield;
  MeleeAbility PunchAbility;
  ShieldAbility ShieldAbility;
  TaskScope MainScope = new();
  bool WasHit = false;

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    TargetAbilities = Target.GetComponent<AbilityManager>();
    Status = GetComponent<Status>();
    Defender = GetComponent<Defender>();
    Hurtbox = GetComponentInChildren<Hurtbox>();
    Shield = GetComponentInChildren<Shield>();
    Abilities = GetComponent<AbilityManager>();
    PunchAbility = GetComponentInChildren<MeleeAbility>();
    ShieldAbility = GetComponentInChildren<ShieldAbility>();
    Hurtbox.OnHurt.Listen((_) => WasHit = true);
  }

  // TODO: What about dash?
  bool TargetIsAttacking { get => TargetAbilities.Abilities.Any((a) => a.IsRunning); }

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
        if (TargetIsAttacking && Shield) {
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
      var desiredMoveDir = Vector3.zero;
      if (!inRange)
        desiredMoveDir = (desiredPos - transform.position).XZ().normalized;
      Mover.UpdateAxes(Abilities, desiredMoveDir, desiredFacing);
      await scope.Tick();
    }

    Mover.UpdateAxes(Abilities, Vector3.zero, transform.forward.XZ());

    if (WasHit) {
      WasHit = false;
      ShieldAbility.Stop();
      await scope.Millis(500);
      return;
    }

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
