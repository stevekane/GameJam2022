using System.Threading.Tasks;
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

  public Ledge Ledge;
  public bool Hanging;
  public bool ClimbingUp;
  public Vector3 HangOffset = new Vector3(0, -2, -1);
  public Vector3 LeftHandHangOffset;
  public Vector3 RightHandHangOffset;

  TaskScope Scope;

  public PlayableGraph Graph;

  void Start() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Listen(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(Jump);
    InputManager.ButtonEvent(SprintAbility.ButtonCode, ButtonPressType.JustDown).Listen(StartSprint);
    Scope = new TaskScope();
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
    var movementMagnitude = 0;
    var worldSpaceDirection = InputManager.Axis(AxisCode.AxisLeft).XZFrom(PersonalCamera.Current);
    if (Hanging) {
      transform.forward = Ledge.Pivot.forward;
      transform.position = Ledge.Pivot.position + transform.TransformVector(HangOffset);
    } else if (ClimbingUp) {
      // transform.forward = Ledge.Pivot.forward;
    } else {
      if (worldSpaceDirection.sqrMagnitude > 0) {
        // TODO: Do we actually change orientation right away like this?
        // TODO: Should localtimescale shit take priority over inputs?
        transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
        movementMagnitude = 1;
      }
    }

    if (Hanging)
      HandIKWeight = 1;

    if (Hanging || ClimbingUp) {
      Controller.enabled = false;
    } else {
      Controller.enabled = true;
    }

    // COMPUTE MOTION OF CHARACTER
    var dt = LocalTime.FixedDeltaTime;
    if (Hanging || ClimbingUp) {
      Velocity.Value = Vector3.zero;
    } else {
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
    }
    Graph.Evaluate(dt);

    // FIRE FIXED FRAME STUFF (REMOVE THIS)
    FixedFrame++;
    FixedTick.Fire();

    // UPDATE THE ANIMATOR (MOVE TO SEPARATE SYSTEM)
    Animator.SetFloat("Speed", movementMagnitude * MovementSpeed.Value);
    Animator.SetBool("Grounded", Grounded.Value);
    Animator.SetBool("Hanging", Hanging);
  }

  void OnAnimatorMove() {
    if (ClimbingUp) {
      transform.position += Animator.deltaPosition;
      transform.rotation *= Animator.deltaRotation;
    }
  }

  float HandIKWeight;
  async Task ClimbUp(TaskScope scope) {
    try {
      Hanging = false;
      ClimbingUp = true;
      Animator.SetTrigger("ClimbUp");
      for (var i = 0; i < 10; i++) {
        HandIKWeight = 1;
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
      HandIKWeight = 0;
      for (var i = 0; i < 20; i++) {
        await scope.ListenFor(LogicalTimeline.FixedTick);
      }
    } finally {
      ClimbingUp = false;
    }
  }

  void OnAnimatorIK() {
    Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, HandIKWeight);
    if (Hanging || ClimbingUp) {
      var leftHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.LeftHand);
      var rightHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.RightHand);
      var leftHandTarget = Ledge.Pivot.position + transform.TransformVector(LeftHandHangOffset);
      var rightHandTarget = Ledge.Pivot.position + transform.TransformVector(RightHandHangOffset);
      var transitionInfo = Animator.GetAnimatorTransitionInfo(0);
      Animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget);
      Animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget);
      Animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(transform.forward));
      Animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(transform.forward));
    }
  }

  void OnLedgeEnter(Ledge l) {
    Ledge = l;
    Hanging = true;
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
    if (Hanging) {
      Scope.Start(ClimbUp);
    } else {
      Scope.Dispose();
      Scope = new();
      Scope.Start(Grounded.Value ? JumpAbility.Jump : DoubleJumpAbility.Jump);
    }
  }

  TaskScope SprintScope = new();
  void StartSprint() {
    InputManager.Consume(ButtonCode.R2, ButtonPressType.JustDown);
    SprintScope.Start(SprintAbility.Activate);
  }
}