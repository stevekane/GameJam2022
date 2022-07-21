using System.Collections;
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
    Attacker.StartAttack(0);
    yield return new WaitUntil(() => !Attacker.IsAttacking);
    yield return new WaitForFixedUpdate();
    Attacker.StartAttack(1);
    yield return new WaitUntil(() => !Attacker.IsAttacking);
    yield return new WaitForFixedUpdate();
    Attacker.StartAttack(0);
    yield return new WaitUntil(() => !Attacker.IsAttacking);
    yield return new WaitForFixedUpdate();
    Attacker.StartAttack(2);
    yield return new WaitUntil(() => !Attacker.IsAttacking);
    yield return new WaitForSeconds(.8f);
    AttackRoutine = null;
  }

  IEnumerator DodgeSequence() {
    yield return new WaitForSeconds(.1f);
    IsDodging = true;
    yield return new WaitUntil(() => !Target.IsAttacking);
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

  Attacker Attacker;
  Defender Defender;
  Cannon Cannon;
  Pushable Pushable;
  Status Status;
  CharacterController Controller;
  Animator Animator;
  AudioSource AudioSource;
  Attacker Target;

  Motion Motion;
  Coroutine AttackRoutine;
  Coroutine DodgeRoutine;
  bool IsDodging = false;

  void Awake() {
    Attacker = GetComponent<Attacker>();
    Defender = GetComponent<Defender>();
    Cannon = GetComponentInChildren<Cannon>();
    Pushable = GetComponent<Pushable>();
    Status = GetComponent<Status>();
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
    Target = GameObject.FindObjectOfType<Player>().GetComponent<Attacker>();
  }

  void FixedUpdate() {
    (Vector3 desiredPos, Vector3 desiredFacing, float distToTarget) = ChoosePosition();
    bool inAttackRange = distToTarget < ATTACK_RANGE;
    bool inMoveRange = distToTarget < ATTACK_RANGE*.9f;
    var dt = Time.fixedDeltaTime;

    var shouldAttack = Status.CanAttack && inAttackRange;
    if (shouldAttack && AttackRoutine == null) {
      AttackRoutine = StartCoroutine(AttackSequence());
    }
    //if (!shouldAttack && AttackRoutine != null) {
    //  StopCoroutine(AttackRoutine);
    //  Attacker.CancelAttack();
    //  AttackRoutine = null;
    //}

    if (Target.IsAttacking && DodgeRoutine == null) {
      DodgeRoutine = StartCoroutine(DodgeSequence());
    }

    Animator.SetBool("Dashing", Motion == Motion.Dashing);
    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);

    var moveSpeed = 0 switch {
      _ when !Status.CanMove => 0,
      _ when inMoveRange => 0,
      _ when Attacker.IsAttacking => Attacker.MoveFactor*MOVE_SPEED,
      _ when Motion == Motion.Dashing => DASH_SPEED,
      _ => MOVE_SPEED
    };
    var velocity = moveSpeed * (desiredPos - transform.position).XZ().normalized;
    velocity += Pushable.Impulse;
    Controller.Move(dt*velocity);
    if (Motion == Motion.Dashing) {
      ChargeParticles.transform.forward = -velocity.TryGetDirection() ?? -transform.forward;
    }

    var turnSpeed = 0 switch {
      _ when Attacker.IsAttacking => ATTACKING_TURN_SPEED,
      _ when Cannon.IsFiring => FIRING_TURN_SPEED,
      _ => TURN_SPEED
    };
    transform.rotation = RotationFromDesired(transform, turnSpeed, desiredFacing, dt);
  }
}