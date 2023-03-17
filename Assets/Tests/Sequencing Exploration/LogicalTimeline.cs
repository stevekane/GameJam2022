using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

public class LogicalTimeline : MonoBehaviour {
  public static int FixedFrame;
  public static EventSource FixedTick = new();

  [Header("Input")]
  [SerializeField] InputManager InputManager;
  [SerializeField] AudioClip FootStepSFX;
  [SerializeField] GameObject FootStepVFX;
  [SerializeField] DecalProjector FootStepDecal;

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
  [SerializeField] SplitGravity Gravity;
  [SerializeField] MaxMovementSpeed MaxMovementSpeed;
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] TurningSpeed TurningSpeed;
  [SerializeField] Acceleration Acceleration;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Velocity Velocity;
  [SerializeField] Traditional.Grounded Grounded;

  TaskScope Scope;

  public PlayableGraph Graph;
  public int HitStopFramesRemaining;

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
    var camera = Camera.main; // TODO: slow way to access camera
    var dt = LocalTime.FixedDeltaTime;
    var movementInput = InputManager.Axis(AxisCode.AxisLeft);
    var screenDirection = movementInput.XY;
    var movementMagnitude = screenDirection.magnitude;
    var worldSpaceDirection = camera.transform.TransformDirection(screenDirection);
    worldSpaceDirection.y = 0;
    worldSpaceDirection = worldSpaceDirection.normalized;
    if (movementMagnitude > 0) {
      transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
      movementMagnitude = 1;
    }

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
    LocalTime.TimeScale = HitStopFramesRemaining > 0 ? 0 : 1;
    HitStopFramesRemaining = Mathf.Max(0, HitStopFramesRemaining-1);
    Graph.Evaluate(dt);
    FixedFrame++;
    FixedTick.Fire();
    Animator.SetFloat("Speed", movementMagnitude * MovementSpeed.Value);
    Animator.SetBool("Grounded", Grounded.Value);
  }

  void OnFootStep(string footName) {
    var targetBone = footName == "Left" ? AvatarBone.LeftFoot : AvatarBone.RightFoot;
    var targetFoot = AvatarAttacher.FindBoneTransform(Animator, targetBone);
    if (MovementSpeed.Value > 10) {
      ResidualImageRenderer.Render();
      Destroy(Instantiate(FootStepVFX, targetFoot.transform.position, targetFoot.transform.rotation), 2);
      Destroy(Instantiate(FootStepDecal, targetFoot.transform.position, Quaternion.LookRotation(Vector3.down)), 2);
    }
    AudioSource.PlayClipAtPoint(FootStepSFX, targetFoot.position);
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
    Scope.Start(Grounded.Value ? JumpAbility.Jump : DoubleJumpAbility.Jump);
  }

  TaskScope SprintScope = new();
  void StartSprint() {
    InputManager.Consume(ButtonCode.R2, ButtonPressType.JustDown);
    SprintScope.Start(SprintAbility.Activate);
  }
}