using System.Collections;
using System.Linq;
using UnityEngine;

public class Smoke : MonoBehaviour {
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
    Abilities.GetEvent(LightAttack.AttackStart).Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    Abilities.GetEvent(LightAttack.AttackStart).Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    Abilities.GetEvent(LightAttack.AttackStart).Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForFixedUpdate();
    Abilities.GetEvent(Slam.ChargeStart).Fire();
    yield return new WaitForSeconds(0.5f);
    Abilities.GetEvent(Slam.ChargeRelease).Fire();
    yield return new WaitUntil(() => !IsAttacking);
    yield return new WaitForSeconds(.8f);
    AttackRoutine = null;
  }

  //IEnumerator DodgeSequence() {
  //  yield return new WaitForSeconds(.1f);
  //  IsDodging = true;
  //  yield return new WaitUntil(() => !TargetIsAttacking);
  //  yield return new WaitForSeconds(.1f);
  //  IsDodging = false;
  //  DodgeRoutine = null;
  //}

  [SerializeField] float ATTACK_RANGE = 8f;

  AbilityManager Abilities;
  Status Status;
  AbilityManager Target;
  MeleeAttackAbility LightAttack;
  SlamAbility Slam;

  Coroutine AttackRoutine;
  //Coroutine DodgeRoutine;
  bool IsDodging = false;
  int RecoveryFrames;

  void Awake() {
    Abilities = GetComponent<AbilityManager>();
    Status = GetComponent<Status>();
    Target = GameObject.FindObjectOfType<Player>().GetComponent<AbilityManager>();
    LightAttack = GetComponentInChildren<MeleeAttackAbility>();
    Slam = GetComponentInChildren<SlamAbility>();
  }

  bool IsAttacking { get => Abilities.Abilities.Any((a) => a.IsRunning); }
  bool TargetIsAttacking { get => Target.Abilities.Any((a) => a.IsRunning); }

  void FixedUpdate() {
    if (Target == null)
      return;

    (Vector3 desiredPos, Vector3 desiredFacing, float distToTarget) = ChoosePosition();
    bool inAttackRange = distToTarget < ATTACK_RANGE;
    bool inMoveRange = distToTarget < ATTACK_RANGE*.9f;

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

    var desiredMoveDir = (desiredPos - transform.position).XZ().normalized;
    if (inMoveRange)
      desiredMoveDir = Vector3.zero;
    Mover.UpdateAxes(Abilities, desiredMoveDir, desiredFacing);
  }
}