using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class SmokeWitch : MonoBehaviour, IMobComponents {
  public float DefensiveRange = 8f;
  public Timeval AttackCooldown = Timeval.FromMillis(1000);
  public Ability GroundLightAttack;
  public Ability GroundHeavyAttack;
  public Ability GroundLauncherAttack;
  public Ability AirSpikeAttack;

  List<AIBehavior> GapCloseBehaviors = new();
  List<AIBehavior> AttackBehaviors = new();
  Status Status;
  Transform Target;
  NavMeshAgent NavMeshAgent;
  Flash Flash;
  Mover Mover;
  AIMover AIMover;
  AbilityManager AbilityManager;
  Throw ThrowAbility;
  BurstDash DashAbility;
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
    InitBehaviors();
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

  void InitBehaviors() {
    void Melee(AIBehavior b) {
      b.Score = 1;
      b.StartRange = (0f, 4f);
      b.DuringRange = (0f, 12f);
      b.DesiredDistance = 3f;
    }
    void Ground(AIBehavior b) {
      b.CanStart = () => Status.IsGrounded;
    }
    void Air(AIBehavior b) {
      b.CanStart = () => !Status.IsGrounded;
    }
    // Ground melee.
    AttackBehaviors.Add(new AIBehavior(this).With(Ground).With(Melee).With((b) => {
      b.Behavior = async (scope) => {
        await b.TelegraphDuringAttack(scope);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, GroundLauncherAttack);
        await scope.Millis(550);
        await b.StartAbility(scope, JumpAbility);
        await b.StartAbilityWhenInRange(scope, AirSpikeAttack);
      };
    }));
    // Ground melee.
    AttackBehaviors.Add(new AIBehavior(this).With(Ground).With(Melee).With((b) => {
      b.Behavior = async (scope) => {
        await b.TelegraphDuringAttack(scope);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, GroundLauncherAttack);
        await b.ReleaseAbilityWhenInRange(scope, GroundHeavyAttack);
      };
    }));
    // Ground melee.
    AttackBehaviors.Add(new AIBehavior(this).With(Ground).With(Melee).With((b) => {
      b.Behavior = async (scope) => {
        await b.TelegraphDuringAttack(scope);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.ReleaseAbilityWhenInRange(scope, GroundHeavyAttack);
      };
    }));
    // Air melee.
    AttackBehaviors.Add(new AIBehavior(this).With(Air).With(Melee).With((b) => {
      b.Cooldown = Timeval.FromMillis(500);
      b.Behavior = async (scope) => {
        await b.TelegraphDuringAttack(scope);
        await b.StartAbilityWhenInRange(scope, AirSpikeAttack);
      };
    }));
    // Air melee.
    AttackBehaviors.Add(new AIBehavior(this).With(Air).With(Melee).With((b) => {
      b.Cooldown = Timeval.FromMillis(500);
      b.Behavior = async (scope) => {
        await b.TelegraphDuringAttack(scope);
        await b.StartAbilityWhenInRange(scope, GroundLightAttack);
        await b.StartAbilityWhenInRange(scope, AirSpikeAttack);
      };
    }));
    // Ranged fireball.
    AttackBehaviors.Add(new AIBehavior(this).With((b) => {
      b.Score = 1;
      b.StartRange = (16f, float.MaxValue);
      b.DuringRange = (14f, float.MaxValue);
      b.DesiredDistance = 18f;
      b.Cooldown = Timeval.FromMillis(1000);
      b.CanStart = () => !InDanger();
      b.Behavior = async (scope) => {
        ThrowAbility.Target = Target;
        await b.StartAbility(scope, ThrowAbility);
      };
    }));
    // Dash in.
    GapCloseBehaviors.Add(new AIBehavior(this).With((b) => {
      b.Score = 1;
      b.StartRange = (6f, 14f);
      b.DuringRange = (0f, float.MaxValue);
      b.DesiredDistance = 2f;
      b.Cooldown = Timeval.FromMillis(300);
      b.Behavior = async (scope) => {
        // Mob move logic should start moving torwards target due to new desired distance.
        await scope.Any(Waiter.Until(() => b.MovingTowardsTarget()), Waiter.Millis(100));
        await b.StartAbility(scope, DashAbility);
        await scope.Until(() => b.TargetInRange(0f, 4f) || !DashAbility.IsRunning);
      };
    }));
  }
}
