using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

using Range = System.ValueTuple<float, float>;

public class SmokeWitch : MonoBehaviour, IMobComponents {
  public float DefensiveRange = 8f;
  public Timeval AttackCooldown = Timeval.FromMillis(1000);
  public Ability GroundLightAttack;
  public Ability GroundHeavyAttack;
  public Ability GroundLauncherAttack;
  public Ability AirSpikeAttack;

  SmokeWitchBehavior[] GapCloseBehaviors;
  SmokeWitchBehavior[] AttackBehaviors;
  Status Status;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Flash Flash;
  Mover Mover;
  AIMover AIMover;
  AbilityManager AbilityManager;
  Throw ThrowAbility;
  SimpleDash DashAbility;
  Jump JumpAbility;
  TaskScope MainScope = new();
  float DesiredDistance => CurrentAttack?.DesiredDistance ?? DefensiveRange;

  Vector3? DesiredPosition = null;

  AbilityManager IMobComponents.AbilityManager => AbilityManager;
  Status IMobComponents.Status => Status;
  Flash IMobComponents.Flash => Flash;
  Mover IMobComponents.Mover => Mover;
  Transform IMobComponents.Target => Target;

  public void Awake() {
    Target = Player.Get().transform;
    this.InitComponent(out Status);
    this.InitComponent(out Mover);
    this.InitComponent(out AIMover);
    this.InitComponent(out NavMeshAgent);
    this.InitComponent(out Flash);
    this.InitComponent(out AbilityManager);
    this.InitComponentFromChildren(out ThrowAbility);
    this.InitComponentFromChildren(out DashAbility);
    this.InitComponentFromChildren(out JumpAbility);
    AttackBehaviors = new SmokeWitchBehavior[] {
      new Melee1(),
      new Melee2(),
      new Melee3(),
      new Air1(),
      new Air2(),
      new ThrowSequence()
    };
    AttackBehaviors.ForEach(s => s.Owner = this);
    GapCloseBehaviors = new SmokeWitchBehavior[] {
      new DashBehavior(),
    };
    GapCloseBehaviors.ForEach(s => s.Owner = this);
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
      Waiter.Repeat(TryGapClose),
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
    Vector3 FindRecoverPosition() {
      var edge = NavMeshUtil.Instance.FindClosestPointOnEdge(transform.position);
      var delta = edge - transform.position;
      return transform.position + delta + 2f*delta.XZ().normalized;
    }
    if (ShouldMove && Status.CanAttack && InDanger()) {
      DesiredPosition = FindRecoverPosition();
      await AbilityManager.TryRun(scope, DashAbility.MainAction);
      if (InDanger()) {
        DesiredPosition = FindRecoverPosition();
        await AbilityManager.TryRun(scope, JumpAbility.MainAction);
        await scope.Until(() => Status.IsGrounded);
      }
      DesiredPosition = null;
    }
  }

  async Task TryReposition(TaskScope scope) {
    NavMeshAgent.SetDestination(ChaseTarget(DesiredDistance));
    await scope.Tick();
  }

  AIBehavior CurrentAttack;
  async Task TryGapClose(TaskScope scope) {
    if (Target && Status.CanAttack && CurrentAttack == null && AIBehavior.ChooseBehavior(GapCloseBehaviors) is var b && b != null) {
      CurrentAttack = b;
      await b.Run(scope);
      if (CurrentAttack == b)  // Can be cancelled by attack sequence
        CurrentAttack = null;
    }
  }

  async Task TryAttackSequence(TaskScope scope) {
    if (Target && Status.CanAttack && AIBehavior.ChooseBehavior(AttackBehaviors) is var b && b != null) {
      CurrentAttack = b;
      await b.Run(scope);
      CurrentAttack = null;
    }
  }

  //AttackSequence CurrentAttack;
  //async Task TryAttackSequence(TaskScope scope) {
  //  if (Target && Status.CanAttack && ChooseAttackSequence() is var seq && seq) {
  //    CurrentAttack = seq;
  //    await seq.Perform(scope, Target);
  //    CurrentAttack = null;
  //    await scope.Delay(AttackCooldown);
  //  }
  //}

  // Choose one of the startable sequences using a weighted random pick.
  //AttackSequence ChooseAttackSequence() {
  //  var usableSequences = AttackSequences.Where(seq => seq.CanStart(Target));
  //  var totalScore = usableSequences.Sum(seq => seq.Score);
  //  var chosenScore = UnityEngine.Random.Range(0f, totalScore);
  //  var score = 0f;
  //  foreach (var seq in usableSequences) {
  //    score += seq.Score;
  //    if (chosenScore <= score)
  //      return seq;
  //  }
  //  return null;
  //}

  Vector3 ChaseTarget(float desiredDist) {
    if (DesiredPosition.HasValue)
      return DesiredPosition.Value;
    var delta = Target.transform.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - NavMeshAgent.stoppingDistance) * delta.normalized;
  }

  bool InDanger() {
    bool OverVoid() {
      const float MAX_RAYCAST_DISTANCE = 1000;
      return !Physics.Raycast(transform.position, Vector3.down, MAX_RAYCAST_DISTANCE, Defaults.Instance.EnvironmentLayerMask);
    }
    return !Status.IsGrounded && OverVoid();
  }

  void OnDrawGizmos() {
    Gizmos.color = Color.magenta;
    if (DesiredPosition.HasValue)
      Gizmos.DrawWireSphere(DesiredPosition.Value, .5f);
  }

  abstract class SmokeWitchBehavior : AIBehavior {
    public SmokeWitch Owner;
    override public IMobComponents Mob => Owner;
  }

  abstract class GroundMeleeBehavior : SmokeWitchBehavior {
    override public int Score => 1;
    override public Range StartRange => (0f, 4f);
    override public Range DuringRange => (0f, 12f);
    public override bool CanStart() => Mob.Status.IsGrounded && base.CanStart();
    protected bool RequiredGrounded = true;
  }

  abstract class AirMeleeBehavior : SmokeWitchBehavior {
    override public int Score => 1;
    override public Range StartRange => (0f, 4f);
    override public Range DuringRange => (0f, 12f);
    public override bool CanStart() => !Mob.Status.IsGrounded && base.CanStart();
  }

  class DashBehavior : SmokeWitchBehavior {
    override public int Score => 1;
    override public Range StartRange => (6f, 14f);
    override public Range DuringRange => (0f, float.MaxValue);
    override public float DesiredDistance => 2f;
    override public Timeval Cooldown => Timeval.FromMillis(300);
    protected override async Task Behavior(TaskScope scope) {
      // Mob move logic should start moving torwards target due to new desired distance.
      await scope.Any(Waiter.Until(() => MovingTowardsTarget()), Waiter.Millis(100));
      await StartAbility(scope, Owner.DashAbility);
      await scope.Until(() => TargetInRange(0f, 4f) || !Owner.DashAbility.IsRunning);
    }
  }

  class ThrowSequence : SmokeWitchBehavior {
    override public int Score => 1;
    override public Range StartRange => (16f, float.MaxValue);
    override public Range DuringRange => (14f, float.MaxValue);
    override public float DesiredDistance => 18f;
    override public Timeval Cooldown => Timeval.FromMillis(1000);
    public override bool CanStart() => !Owner.InDanger() && base.CanStart();
    protected override async Task Behavior(TaskScope scope) {
      Owner.ThrowAbility.Target = Mob.Target;
      await StartAbility(scope, Owner.ThrowAbility);
    }
  }

  class Melee1 : GroundMeleeBehavior {
    protected override async Task Behavior(TaskScope scope) {
      await TelegraphDuringAttack(scope);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.GroundLauncherAttack);
      await scope.Millis(550);
      await StartAbility(scope, Owner.JumpAbility);
      await StartAbilityWhenInRange(scope, Owner.AirSpikeAttack);
    }
  }

  class Melee2 : GroundMeleeBehavior {
    override public int Score => 100000;
    protected override async Task Behavior(TaskScope scope) {
      await TelegraphDuringAttack(scope);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.GroundLauncherAttack);
      await ReleaseAbilityWhenInRange(scope, Owner.GroundHeavyAttack);
    }
  }

  class Melee3 : GroundMeleeBehavior {
    protected override async Task Behavior(TaskScope scope) {
      await TelegraphDuringAttack(scope);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await ReleaseAbilityWhenInRange(scope, Owner.GroundHeavyAttack);
    }
  }

  class Air1 : AirMeleeBehavior {
    override public Timeval Cooldown => Timeval.FromMillis(500);
    protected override async Task Behavior(TaskScope scope) {
      await TelegraphDuringAttack(scope);
      await StartAbilityWhenInRange(scope, Owner.AirSpikeAttack);
    }
  }

  class Air2 : AirMeleeBehavior {
    override public Timeval Cooldown => Timeval.FromMillis(500);
    protected override async Task Behavior(TaskScope scope) {
      await TelegraphDuringAttack(scope);
      await StartAbilityWhenInRange(scope, Owner.GroundLightAttack);
      await StartAbilityWhenInRange(scope, Owner.AirSpikeAttack);
    }
  }
}