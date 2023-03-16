using UnityEngine;
using UnityEngine.Playables;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;
  [SerializeField] float MovementSpeed = 10;

  [Header("Visual Effects")]
  [SerializeField] GameObject OnHitVFX;

  [Header("Components")]
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] MeleeAttackAbility ThreeHitComboAbility;
  [SerializeField] JumpAbility JumpAbility;
  [SerializeField] DoubleJumpAbility DoubleJumpAbility;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  [Header("State")]
  [SerializeField] SplitGravity Gravity;
  [SerializeField] float AirAcceleration = 1;
  [SerializeField] float MaxAirSpeed = 15;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Velocity Velocity;

  TaskScope Scope;

  public PlayableGraph Graph;
  public int HitStopFramesRemaining;

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Scope = new TaskScope();
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(Jump);
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(Jump);
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    var dt = LocalTime.FixedDeltaTime;
    var movementInput = InputManager.Axis(AxisCode.AxisLeft);
    var screenDirection = movementInput.XY;
    var movementMagnitude = screenDirection.magnitude;
    var camera = Camera.main; // TODO: slow way to access camera
    var worldSpaceDirection = camera.transform.TransformDirection(screenDirection);
    worldSpaceDirection.y = 0;
    worldSpaceDirection = worldSpaceDirection.normalized;
    if (movementMagnitude > 0) {
      transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
    }
    var planeVelocity = Vector3.zero;
    if (Controller.isGrounded) {
      planeVelocity = movementMagnitude * MovementSpeed * worldSpaceDirection;
      if (Velocity.Value.y > 0) {
        Velocity.Value.y += dt * Gravity.Value;
      } else {
        Velocity.Value.y = dt * Gravity.Value;
      }
    } else {
      var airVelocity = Velocity.Value + dt * AirAcceleration * movementMagnitude * worldSpaceDirection;
      airVelocity.y = 0;
      var airSpeed = airVelocity.magnitude;
      var airDirection = airVelocity.normalized;
      planeVelocity = Mathf.Min(MaxAirSpeed, airSpeed) * airDirection;
      Velocity.Value.y += dt * Gravity.Value;
    }
    Velocity.Value.x = planeVelocity.x;
    Velocity.Value.z = planeVelocity.z;
    Controller.Move(dt * Velocity.Value);
    LocalTime.TimeScale = HitStopFramesRemaining > 0 ? 0 : 1;
    HitStopFramesRemaining = Mathf.Max(0, HitStopFramesRemaining-1);
    Graph.Evaluate(dt);
    FixedFrame++;
    FixedTick.Fire();
    Animator.SetFloat("Speed", movementMagnitude);
    Animator.SetBool("Grounded", Controller.isGrounded);
  }

  void OnHit(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks;
  }

  void OnBlocked(MeleeContact contact) {
    MeleeAttackTargeting.Victims.Add(contact.Hurtbox.Owner.gameObject);
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHit(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks / 2;
  }

  void OnParried(MeleeContact contact) {
    Destroy(Instantiate(OnHitVFX, contact.Hurtbox.transform.position + Vector3.up, transform.rotation), 3);
    Vibrator.VibrateOnHurt(transform.forward, contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2);
    HitStopFramesRemaining = contact.Hitbox.HitboxParams.HitStopDuration.Ticks * 2;
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
    if (Controller.isGrounded) {
      Scope.Start(JumpAbility.Jump);
    } else {
      Scope.Start(DoubleJumpAbility.Jump);
    }
  }
}