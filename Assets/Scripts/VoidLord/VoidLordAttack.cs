using UnityEngine;

public class VoidLordAttack : VoidLordState {
  [SerializeField] int AttackIndex;
  [SerializeField] Timeval MaxDuration;
  [SerializeField] VoidLordState Done;

  int Duration;

  public override void OnEnter(VoidLord voidLord) {
    Duration = 0;
  }

  public override void Step(VoidLord voidlord, Action action, float dt) {
    Duration++;
    voidlord.Animator.SetInteger(VoidLord.ACTION_INDEX, 0);
    voidlord.Animator.SetInteger(VoidLord.ATTACK_INDEX, AttackIndex);
    voidlord.Animator.SetBool(VoidLord.IS_ATTACKING, true);
    voidlord.Animator.SetFloat(VoidLord.ATTACK_SPEED, 1);
    if (Duration >= MaxDuration.Frames) {
      voidlord.Transition(this, Done);
    }
  }
}