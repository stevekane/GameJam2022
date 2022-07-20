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
  int WaitFrames = 1000;

  enum StateType { Idle, Chase, Attack, Block }

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    TargetAttacker = Target.GetComponent<Attacker>();
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Attacker = GetComponent<Attacker>();
    Defender = GetComponent<Defender>();
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

    if (Status.CanAttack && !Attacker.IsAttacking) {
      if (TargetAttacker.IsAttacking) {
        Attacker.StartHoldAttack(1);
        WaitFrames = Timeval.FromMillis(1000).Frames;
      } else if (inRange) {
        Attacker.StartAttack(0);
        WaitFrames = AttackDelay.Frames;
      }
    }
    if (Defender.IsBlocking) {
      if (TargetAttacker.IsAttacking)
        WaitFrames = Timeval.FromMillis(1000).Frames;
      if (WaitFrames <= 0)
        Attacker.ReleaseHoldAttack();
    }

    if (!inRange && Status.CanMove && !Attacker.IsAttacking && WaitFrames <= 0) {
      transform.forward = desiredFacing;
      var dir = (desiredPos - transform.position).normalized;
      Controller.Move(dir * MoveSpeed * Time.fixedDeltaTime);
    }

    if ((!Attacker.IsAttacking || Defender.IsBlocking) && WaitFrames > 0)
      --WaitFrames;

    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);
    Animator.SetBool("HitFlinch", Status.IsHitstun);
    Defender.IsBlocking = Attacker.AttackIndex == 1 && Attacker.AttackState == AttackState.Active;
  }
}
