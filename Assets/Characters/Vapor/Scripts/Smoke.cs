using System.Collections;
using System.Linq;
using UnityEngine;

public class Smoke : MonoBehaviour {
  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward, float dt) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = dt*speed;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  (Vector3, Vector3, float) ChoosePosition() {
    var t = Target.transform;
    var pos = t.position;
    var delta = (t.position - transform.position).XZ();
    if (IsDodging)
      pos -= delta.normalized*(ATTACK_RANGE + 10f);
    return (pos, delta.normalized, delta.magnitude);
  }

  IEnumerator AttackSequence() {
    yield return new WaitForSeconds(.1f);
    LightAttack.Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    LightAttack.Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    LightAttack.Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    SlamStart.Fire();
    yield return new WaitForSeconds(0.5f);
    SlamRelease.Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForSeconds(.8f);
    AttackRoutine = null;
  }

  IEnumerator DodgeSequence() {
    yield return new WaitForSeconds(.1f);
    IsDodging = true;
    yield return new WaitUntil(() => !TargetIsAttacking);
    yield return new WaitForSeconds(.1f);
    IsDodging = false;
    DodgeRoutine = null;
  }

  [SerializeField] float GRAVITY;
  [SerializeField] float MOVE_SPEED;
  [SerializeField] float FIRING_MOVE_SPEED;
  [SerializeField] float DASH_SPEED;
  [SerializeField] float TURN_SPEED;
  [SerializeField] float FIRING_TURN_SPEED;
  [SerializeField] float ATTACKING_TURN_SPEED;
  [SerializeField] float ATTACK_RANGE = 8f;
  [SerializeField] float FIRING_PUSHBACK_SPEED;
  [SerializeField] ParticleSystem ChargeParticles;
  [SerializeField] AudioClip ChargeAudioClip;
  [SerializeField] float ChargeAudioClipStartingTime;

  AbilityManager Abilities;
  Defender Defender;
  Cannon Cannon;
  Status Status;
  CharacterController Controller;
  Animator Animator;
  AudioSource AudioSource;
  AbilityManager Target;

  Motion Motion;
  Coroutine AttackRoutine;
  Coroutine DodgeRoutine;
  bool IsDodging = false;
  Vector3 Velocity;
  int RecoveryFrames;
  EventSource LightAttack = new();
  EventSource HeavyStart = new();
  EventSource HeavyRelease = new();
  EventSource SlamStart = new();
  EventSource SlamRelease = new();
  AxisState MoveAxis = new();
  AxisState AimAxis = new();

  void Awake() {
    Abilities = GetComponent<AbilityManager>();
    Defender = GetComponent<Defender>();
    Cannon = GetComponentInChildren<Cannon>();
    Status = GetComponent<Status>();
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
    Target = GameObject.FindObjectOfType<Player>().GetComponent<AbilityManager>();

    Abilities.RegisterTag(EventTag.LightAttack, LightAttack);
    Abilities.RegisterTag(EventTag.HeavyStart, HeavyStart);
    Abilities.RegisterTag(EventTag.HeavyRelease, HeavyRelease);
    Abilities.RegisterTag(EventTag.SlamStart, SlamStart);
    Abilities.RegisterTag(EventTag.SlamRelease, SlamRelease);
    // TODO: generic mobaxis component that inputs and processes these?
    Abilities.RegisterTag(EventTag.MoveAxis, MoveAxis);
    Abilities.RegisterTag(EventTag.AimAxis, AimAxis);
  }

  bool IsAttacking { get => Abilities.Abilities.Any((a) => a.IsRunning); }
  bool TargetIsAttacking { get => Target.Abilities.Any((a) => a.IsRunning); }

  void FixedUpdate() {
    if (Target == null)
      return;

    (Vector3 desiredPos, Vector3 desiredFacing, float distToTarget) = ChoosePosition();
    bool inAttackRange = distToTarget < ATTACK_RANGE;
    bool inMoveRange = distToTarget < ATTACK_RANGE*.9f;
    var dt = Time.fixedDeltaTime;

    var shouldAttack = Status.CanAttack && inAttackRange && RecoveryFrames <= 0;
    if (shouldAttack && AttackRoutine == null) {
      AttackRoutine = StartCoroutine(AttackSequence());
    }
    //if (!shouldAttack && AttackRoutine != null) {
    //  StopCoroutine(AttackRoutine);
    //  Attacker.CancelAttack();
    //  AttackRoutine = null;
    //}

    //if (Target.IsAttacking && DodgeRoutine == null) {
    //  DodgeRoutine = StartCoroutine(DodgeSequence());
    //}

    if (Status.Get<KnockbackEffect>() != null) {
      RecoveryFrames = Timeval.FromMillis(1000).Frames;
      if (AttackRoutine != null) {
        StopCoroutine(AttackRoutine);
        AttackRoutine = null;
      }
    } else if (RecoveryFrames > 0) {
      --RecoveryFrames;
    }

    Animator.SetBool("Dashing", Motion == Motion.Dashing);

    var moveSpeed = 0 switch {
      _ when !Status.CanMove => 0,
      _ when inMoveRange => 0,
      _ when IsAttacking => .5f*MOVE_SPEED,
      _ when Motion == Motion.Dashing => DASH_SPEED,
      _ => MOVE_SPEED
    };
    Velocity.SetXZ(moveSpeed * (desiredPos - transform.position).XZ().normalized);
    Velocity.y = Controller.isGrounded ? GRAVITY*dt : Velocity.y + GRAVITY*dt;
    Controller.Move(dt*Velocity);
    if (Motion == Motion.Dashing) {
      ChargeParticles.transform.forward = -Velocity.TryGetDirection() ?? -transform.forward;
    }

    var turnSpeed = 0 switch {
      _ when IsAttacking => ATTACKING_TURN_SPEED,
      _ when Cannon.IsFiring => FIRING_TURN_SPEED,
      _ => TURN_SPEED
    };
    transform.rotation = RotationFromDesired(transform, turnSpeed, desiredFacing, dt);
  }
}