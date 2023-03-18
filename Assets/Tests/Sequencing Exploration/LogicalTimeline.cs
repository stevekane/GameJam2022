using UnityEngine;
using UnityEngine.Playables;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Abilities")]
  [SerializeField] MeleeAttackAbility ThreeHitComboAbility;
  [SerializeField] JumpAbility JumpAbility;
  [SerializeField] DoubleJumpAbility DoubleJumpAbility;
  [SerializeField] SprintAbility SprintAbility;

  [Header("Components")]
  [SerializeField] Animator Animator;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] CharacterController Controller;
  [SerializeField] ResidualImageRenderer ResidualImageRenderer;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  [Header("State")]
  [SerializeField] PersonalCamera PersonalCamera;
  [SerializeField] SplitGravity Gravity;
  [SerializeField] MaxMovementSpeed MaxMovementSpeed;
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] TurningSpeed TurningSpeed;
  [SerializeField] Acceleration Acceleration;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Velocity Velocity;
  [SerializeField] Traditional.Grounded Grounded;
  [SerializeField] HitStop HitStop;

  TaskScope Scope;

  public PlayableGraph Graph;

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(Jump);
    InputManager.ButtonEvent(SprintAbility.ButtonCode, ButtonPressType.JustDown).Listen(StartSprint);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(Jump);
    InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Unlisten(StartSprint);
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    var worldSpaceDirection = InputManager.Axis(AxisCode.AxisLeft).XZFrom(PersonalCamera.Current);
    var movementMagnitude = 0;
    if (worldSpaceDirection.sqrMagnitude > 0) {
      // TODO: Do we actually change orientation right away like this?
      // TODO: Should localtimescale shit take priority over inputs?
      transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
      movementMagnitude = 1;
    }

    // UPDATE LOCAL TIME and systems which effect it
    LocalTime.TimeScale = HitStop.TicksRemaining > 0 ? 0 : 1;
    HitStop.TicksRemaining = Mathf.Max(0, HitStop.TicksRemaining-1);

    // COMPUTE MOTION OF CHARACTER
    var dt = LocalTime.FixedDeltaTime;
    var planeVelocity = Vector3.zero;
    if (Grounded.Value) {
      planeVelocity = movementMagnitude * MovementSpeed.Value * worldSpaceDirection;
      if (Velocity.Value.y > 0) {
        Velocity.Value.y += dt * Gravity.Value;
      } else {
        Velocity.Value.y = dt * Gravity.Value;
      }
    } else {
      var airVelocity = Velocity.Value + dt * Acceleration.Value * movementMagnitude * worldSpaceDirection;
      airVelocity.y = 0;
      var airSpeed = airVelocity.magnitude;
      var airDirection = airVelocity.normalized;
      planeVelocity = Mathf.Min(MaxMovementSpeed.Value, airSpeed) * airDirection;
      Velocity.Value.y += dt * Gravity.Value;
    }
    Velocity.Value.x = planeVelocity.x;
    Velocity.Value.z = planeVelocity.z;
    Controller.Move(dt * Velocity.Value);
    Graph.Evaluate(dt);

    // FIRE FIXED FRAME STUFF (REMOVE THIS)
    FixedFrame++;
    FixedTick.Fire();

    // UPDATE THE ANIMATOR (MOVE TO SEPARATE SYSTEM)
    Animator.SetFloat("Speed", movementMagnitude * MovementSpeed.Value);
    Animator.SetBool("Grounded", Grounded.Value);
  }

  void OnHit(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
  }

  void OnBlocked(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
  }

  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStop.TicksRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
    Scope.Dispose();
    Scope = new();
    Debug.Log("OnParried");
    Animator.SetTrigger("Parried");
  }

  void StartAttack() {
    InputManager.Consume(ButtonCode.West, ButtonPressType.JustDown);
    Scope.Dispose();
    Scope = new();
    Scope.Start(ThreeHitComboAbility.Attack);
  }

  void Jump() {
    InputManager.Consume(ButtonCode.South, ButtonPressType.JustDown);
    Scope.Dispose();
    Scope = new();
    Scope.Start(Grounded.Value ? JumpAbility.Jump : DoubleJumpAbility.Jump);
  }

  TaskScope SprintScope = new();
  void StartSprint() {
    InputManager.Consume(ButtonCode.R2, ButtonPressType.JustDown);
    SprintScope.Start(SprintAbility.Activate);
  }
}