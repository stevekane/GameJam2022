using UnityEngine;

public class VoidLordAttack : VoidLordState {
  [SerializeField] int AttackIndex;
  [SerializeField] Timeval MaxDuration;
  [SerializeField] VoidLordState Done;

  int Duration;

  public override void OnEnter(VoidLord voidLord) {
    Duration = 0;
    voidLord.Attacker.StartAttack(AttackIndex);
  }

  public override void Step(VoidLord voidlord, Action action, float dt) {
    Duration++;
    voidlord.Attacker.Step(dt);
    voidlord.Animator.SetInteger(VoidLord.ACTION_INDEX, 0);
    voidlord.Animator.SetInteger(VoidLord.ATTACK_INDEX, voidlord.Attacker.AttackIndex);
    voidlord.Animator.SetBool(VoidLord.IS_ATTACKING, voidlord.Attacker.IsAttacking);
    voidlord.Animator.SetFloat(VoidLord.ATTACK_SPEED, voidlord.Attacker.AttackSpeed);
    if (!voidlord.Attacker.IsAttacking) {
      voidlord.Transition(this, Done);
    }
  }
}