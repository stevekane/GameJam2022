using UnityEngine;

public enum ChargeAttack { Melee, Ranged }  

public class VoidLordCharge : VoidLordState {
  [SerializeField] int ActionIndex;
  [SerializeField] ChargeAttack ChargeAttack;
  [SerializeField] Timeval LightThreshold;
  [SerializeField] Timeval MediumThreshold;
  [SerializeField] VoidLordState LightState;
  [SerializeField] VoidLordState MediumState;
  [SerializeField] VoidLordState HeavyState;

  int Duration;

  bool ShouldRelease(VoidLord voidLord, Action action) => ChargeAttack switch {
    ChargeAttack.Melee => !action.West.Down,
    ChargeAttack.Ranged => !action.East.Down,
    _ => false
  };

  public override void OnEnter(VoidLord voidLord) {
    Duration = 0;
  }

  public override void Step(VoidLord voidlord, Action action, float dt) {
    Duration++;
    voidlord.Animator.SetInteger(VoidLord.ACTION_INDEX, ActionIndex);
    voidlord.Animator.SetInteger(VoidLord.ATTACK_INDEX, Duration switch {
      _ when Duration < LightThreshold.Frames => 0,
      _ when Duration < MediumThreshold.Frames => 1,
      _ => 2
    });
    voidlord.Animator.SetBool(VoidLord.IS_ATTACKING, false);
    voidlord.Animator.SetFloat(VoidLord.ATTACK_SPEED, 1);
    if (ShouldRelease(voidlord, action)) {
      if (Duration < LightThreshold.Frames) {
        voidlord.Transition(this, LightState);
      } else if (Duration < MediumThreshold.Frames) {
        voidlord.Transition(this, MediumState);
      } else {
        voidlord.Transition(this, HeavyState);
      }
    }
  }
}