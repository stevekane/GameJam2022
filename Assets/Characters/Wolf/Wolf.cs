using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;

public class Wolf : MonoBehaviour {
  public float DashRange = 8f;
  public float AttackRange = 3f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  Status Status;
  Hurtbox Hurtbox;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Mover Mover;
  AbilityManager Abilities;
  CycleAbility LightAbility;
  AttackAbility HeavyAbility;
  SwipeDash DashAbility;
  TaskScope MainScope = new();

  public static InlineEffect HurtStunEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "WolfHurt");

  public void Awake() {
    Target = Player.Get().transform;
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out NavMeshAgent);
    this.InitComponentFromChildren(out Hurtbox);
    this.InitComponent(out Abilities);
    this.InitComponentFromChildren(out LightAbility);
    this.InitComponentFromChildren(out HeavyAbility);
    this.InitComponentFromChildren(out DashAbility);
    Hurtbox.OnHurt.Listen(async _ => {
      using var effect = Status.Add(HurtStunEffect);
      await MainScope.Millis(500);
    });
  }

  Vector3 ChoosePosition(float desiredDist) {
    var t = Target.transform;
    var delta = t.position.XZ() - transform.position.XZ();
    return transform.position + delta - desiredDist * delta.normalized;
  }
  bool IsInRange(Vector3 pos) {
    var delta = (pos - transform.position).XZ();
    return delta.sqrMagnitude < .1f;
  }
  bool TargetInRange() {
    var delta = (Target.position - transform.position);
    var dist = AttackRange;
    return delta.y < dist && delta.XZ().sqrMagnitude < dist*dist;
  }

  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    if (Target == null) {
      MainScope.Cancel();
      return;
    }

    Vector3 desiredPos, desiredFacing;

    async Task MaybeAttack(TaskScope scope) {
      if (Status.CanAttack && IsInRange(desiredPos)) {
        var didHit = false;
        DashAbility.Target = Target;
        DashAbility.OnHit = _ => didHit = true;  // TODO: check if the player was the collider?
        await Abilities.TryRun(scope, DashAbility.MainAction);
        if (didHit) {
          await scope.Millis(100);
          await scope.Any(
            FinisherSequence,
            Waiter.Until(() => Status.IsHurt),
            Waiter.Repeat(() => UpdatePos(AttackRange*.7f)),
            Waiter.Repeat(Move));
        }
        await scope.Delay(AttackDelay);
      }
    }

    async Task FinisherSequence(TaskScope scope) {
      await scope.Until(() => TargetInRange() && Status.CanAttack);
      await Abilities.TryRun(scope, LightAbility.MainAction);
      await scope.Until(() => TargetInRange() && Status.CanAttack);
      await Abilities.TryRun(scope, LightAbility.MainAction);
      await scope.Until(() => TargetInRange() && Status.CanAttack);
      await Abilities.TryRun(scope, HeavyAbility.MainAction);
    }

    void UpdatePos(float desiredDist) {
      desiredPos = ChoosePosition(desiredDist);
      desiredFacing = (Target.position - transform.position).XZ().normalized;
    }

    async Task Move(TaskScope scope) {
      NavMeshAgent.SetDestination(desiredPos);
      Mover.SetMoveFromNavMeshAgent();
      Mover.SetAim(desiredFacing);
      await scope.Tick();
    }

    UpdatePos(DashRange);
    Mover.SetMoveAim(Vector3.zero, desiredFacing);
    await MaybeAttack(scope);
    await Move(scope);
  }
}
