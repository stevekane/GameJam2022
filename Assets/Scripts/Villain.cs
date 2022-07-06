using UnityEngine;

public class Villain : MonoBehaviour {
  CharacterController Controller;
  Attacker Attacker;
  Status Status;
  Animator Animator;
  Transform Target;
  int AttackCheckFrames = 1000;
  public Timeval AttackDelay;
  public float AttackRange = 2f;
  public float MoveSpeed = 3f;

  private void Awake() {
    Controller = GetComponent<CharacterController>();
    Attacker = GetComponentInChildren<Attacker>();
    Status = GetComponent<Status>();
    Animator = GetComponent<Animator>();
    Target = FindObjectOfType<Player>()?.transform;
  }

  void FixedUpdate() {
    if (Status.CanAttack && !Attacker.IsAttacking && --AttackCheckFrames <= 0) {
      AttackCheckFrames = AttackDelay.Frames;
      if (IsTargetInRange(AttackRange) && MaybeChooseAttack(out int which)) {
        Attacker.StartAttack(which);
      }
    }
    Attacker.Step(Time.fixedDeltaTime);

    Vector3 dir = (Target.position - transform.position).XZ().normalized;
    float speed = 0;
    if (Status.CanMove && !IsTargetInRange(AttackRange * .8f)) {
      speed = MoveSpeed;
    }

    {
      transform.forward = dir;
      Controller.Move(dir * speed * Time.fixedDeltaTime);
      Animator.SetInteger("LegState", 0);
      Animator.SetFloat("Forward", speed);
      Animator.SetFloat("Right", 0.0f);
    }
  }

  bool IsTargetInRange(float range) {
    return (Target.position - transform.position).XZ().sqrMagnitude < range*range;
  }

  bool MaybeChooseAttack(out int which) {
    var shouldAttack = Random.Range(0, 1f) < .5f;
    which = Random.Range(0, 3);
    return shouldAttack;
  }
}
