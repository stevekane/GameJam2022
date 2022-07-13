using UnityEngine;

public class VoidLordBase : VoidLordState {
  [SerializeField] VoidLordCharge MeleeCharge; 
  [SerializeField] VoidLordCharge RangedCharge; 

  bool ShouldChargeMelee(VoidLord voidLord, Action action) => action.West.Down;

  bool ShouldChargeRanged(VoidLord voidLord, Action action) => action.East.Down;

  public override void Step(VoidLord voidLord, Action action, float dt) {
    voidLord.transform.rotation = VoidLord.RotationFromInputs(voidLord, action, dt);
    voidLord.Velocity = VoidLord.VelocityFromMove(action, VoidLord.MOVE_SPEED);
    voidLord.Controller.Move(dt*voidLord.Velocity);
    voidLord.Animator.SetInteger(VoidLord.ACTION_INDEX, -1);
    voidLord.Animator.SetInteger(VoidLord.ATTACK_INDEX, -1);
    voidLord.Animator.SetBool(VoidLord.IS_ATTACKING, false);
    voidLord.Animator.SetFloat(VoidLord.ATTACK_SPEED, 1);
    if (ShouldChargeMelee(voidLord, action)) {
      voidLord.Transition(this, MeleeCharge);
    } else if (ShouldChargeRanged(voidLord, action)) {
      voidLord.Transition(this, RangedCharge);
    }
  }
}