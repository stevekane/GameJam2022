using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

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
  [SerializeField] LocalTime LocalTime;
  [SerializeField] SplitGravity Gravity;
  [SerializeField] MaxMovementSpeed MaxMovementSpeed;
  [SerializeField] MaxFallSpeed MaxFallSpeed;
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] TurningSpeed TurningSpeed;
  [SerializeField] Acceleration Acceleration;
  [SerializeField] Velocity Velocity;
  [SerializeField] Traditional.Grounded Grounded;
  [SerializeField] Traditional.GroundDistance GroundDistance;
  [SerializeField] HitStop HitStop;

  public Ledge Ledge;
  public bool Hanging;
  public float HangAngle;
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
    InputManager.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Listen(ReloadWholeScene);
    Scope = new TaskScope();
    Graph = PlayableGraph.Create("Logical Timeline");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
  }

  void ReloadWholeScene() {
    InputManager.Consume(ButtonCode.L2, ButtonPressType.JustDown);
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode.West, ButtonPressType.JustDown).Unlisten(StartAttack);
    InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(Jump);
    InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Unlisten(StartSprint);
    InputManager.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Unlisten(ReloadWholeScene);
    Scope.Dispose();
    Graph.Destroy();
  }

  void FixedUpdate() {
    var movementMagnitude = 0;
    var worldSpaceDirection = InputManager.Axis(AxisCode.AxisLeft).XZFrom(PersonalCamera.Current);
    if (Hanging) {
      transform.forward = Ledge.Pivot.forward;
      transform.position = Ledge.Pivot.position + transform.TransformVector(HangOffset);
    } else {
      if (worldSpaceDirection.sqrMagnitude > 0) {
        // TODO: Do we actually change orientation right away like this?
        // TODO: Should localtimescale shit take priority over inputs?
        transform.rotation = Quaternion.LookRotation(worldSpaceDirection);
        movementMagnitude = 1;
      }
    }

    // FIRE FIXED FRAME STUFF (REMOVE THIS)
    FixedFrame++;
    FixedTick.Fire();

    if (Hanging) {
      HandIKWeight = 1;
      Controller.enabled = false;
    } else {
      HandIKWeight = 0;
      Controller.enabled = true;
    }

    // COMPUTE MOTION OF CHARACTER
    var dt = LocalTime.FixedDeltaTime;
    if (Hanging) {
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
      if (Velocity.Value.y < 0) {
        Velocity.Value.y = -Mathf.Min(Mathf.Abs(MaxFallSpeed.Value), Mathf.Abs(Velocity.Value.y));
      }
      Velocity.Value.x = planeVelocity.x;
      Velocity.Value.z = planeVelocity.z;
      Controller.Move(dt * Velocity.Value);
    }
    Graph.Evaluate(dt);

    // UPDATE THE ANIMATOR (MOVE TO SEPARATE SYSTEM)
    Animator.SetFloat("Speed", movementMagnitude * MovementSpeed.Value);
    Animator.SetFloat("YSpeed", Velocity.Value.y);
    Animator.SetBool("Grounded", Grounded.Value);
    Animator.SetFloat("GroundDistance", GroundDistance.Value);
    Animator.SetBool("Hanging", Hanging);
  }

  float HandIKWeight;

  void OnAnimatorIK() {
    Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, HandIKWeight);
    Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, HandIKWeight);
    if (Hanging) {
      var leftHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.LeftHand);
      var rightHand = AvatarAttacher.FindBoneTransform(Animator, AvatarBone.RightHand);
      var leftHandTarget = Ledge.Pivot.position + transform.TransformVector(LeftHandHangOffset);
      var rightHandTarget = Ledge.Pivot.position + transform.TransformVector(RightHandHangOffset);
      Animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget);
      Animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget);
      Animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(transform.forward));
      Animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(transform.forward));
    }
  }

  void OnLedgeEnter(Ledge l) {
    Ledge = l;
  }

  void OnLedgeExit(Ledge l) {
    Ledge = null;
  }

  void OnLedgeStay(Ledge l) {
    if (!Hanging) {
      var validAngle = Vector3.Angle(transform.forward, l.Pivot.forward) <= HangAngle;
      var isFalling = Velocity.Value.y < -2;
      Hanging = validAngle && isFalling;
    }
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
    Hanging = false;
    Scope.Dispose();
    Scope = new();
    if (Grounded.Value) {
      Scope.Start(JumpAbility.Jump);
    } else {
      Scope.Start(DoubleJumpAbility.Jump);
    }
  }

  TaskScope SprintScope = new();
  void StartSprint() {
    InputManager.Consume(ButtonCode.R2, ButtonPressType.JustDown);
    SprintScope.Start(SprintAbility.Activate);
  }
}