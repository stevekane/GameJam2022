using UnityEngine;

public class Badger : MonoBehaviour {
  public float MoveSpeed = 15f;
  public float AttackRange = 2f;
  public Timeval AttackDelay = Timeval.FromMillis(1000);
  CharacterController Controller;
  Status Status;
  Animator Animator;
  Attacker Attacker;
  Defender Defender;
  Transform Target;
  Attacker TargetAttacker;
  Shield Shield;
  int WaitFrames = 1000;
  int RecoveryFrames = 0;
  Vector3 Velocity;

  enum StateType { Idle, Chase, Attack, Block }

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    TargetAttacker = Target.GetComponent<Attacker>();
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Attacker = GetComponent<Attacker>();
    Defender = GetComponent<Defender>();
    Shield = GetComponentInChildren<Shield>();
  }

  Vector3 ChoosePosition() {
    var t = Target.transform;
    var d = AttackRange*.9f;
    var choices = new[] { t.position + t.right*d, t.position - t.right*d, t.position - t.forward*d };
    var closest = choices[0];
    foreach (var p in choices) {
      if ((p - transform.position).XZ().sqrMagnitude < (p - closest).XZ().sqrMagnitude)
        closest = p;
    }
    return closest;
  }

  bool IsInRange(Vector3 pos) {
    var delta = (pos - transform.position).XZ();
    return delta.sqrMagnitude < .1f;
  }

  void FixedUpdate() {
    var desiredPos = ChoosePosition();
    var desiredFacing = (Target.position - transform.position).XZ().normalized;
    var inRange = IsInRange(desiredPos);

    if (Shield && !Shield.isActiveAndEnabled)
      Shield = null;

    if (Status.CanAttack && !Attacker.IsAttacking && RecoveryFrames <= 0) {
      if (TargetAttacker.IsAttacking && Shield) {
        Attacker.StartHoldAttack(1);
        Animator.SetBool("Held", true);
        WaitFrames = Timeval.FromMillis(1000).Frames;
      } else if (inRange) {
        Attacker.StartAttack(0);
        WaitFrames = AttackDelay.Frames;
      }
    }
    if (Defender.IsBlocking) {
      if (!Shield) {
        Attacker.ReleaseHoldAttack();
        Animator.SetBool("Held", false);
      } else {
        if (TargetAttacker.IsAttacking)
          WaitFrames = Timeval.FromMillis(1000).Frames;
        if (WaitFrames <= 0) {
          Attacker.ReleaseHoldAttack();
          Animator.SetBool("Held", false);
        }
      }
    }

    if (!inRange && Status.CanMove && !Attacker.IsAttacking && WaitFrames <= 0 && RecoveryFrames <= 0) {
      transform.forward = desiredFacing;
      var dir = (desiredPos - transform.position).normalized;
      Velocity.SetXZ(MoveSpeed * dir);
      var gravity = -200f * Time.fixedDeltaTime;
      Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
      Controller.Move(Velocity*Time.fixedDeltaTime);
    }

    if ((!Attacker.IsAttacking || Defender.IsBlocking) && WaitFrames > 0)
      --WaitFrames;
    if (Status.IsHitstun) {
      RecoveryFrames = Timeval.FromMillis(1000).Frames;
      Animator.SetBool("Held", false);
    } else if (RecoveryFrames > 0) {
      --RecoveryFrames;
    }

    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);
    Animator.SetBool("HitFlinch", Status.IsHitstun);
    Defender.IsBlocking = Attacker.AttackIndex == 1 && Attacker.AttackState == AttackState.Active;
    if (Shield)
      Shield.Defender.IsBlocking = !Defender.IsBlocking;
  }
}
