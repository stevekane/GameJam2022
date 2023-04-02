using UnityEngine;

namespace ActionsAndAI {
  public class DoubleJump : SimpleAbility {
    [SerializeField] Velocity Velocity;
    [SerializeField] float Strength = 35;

    public override void OnRun() {
      Velocity.Value.y = Strength;
      SimpleAbilityManager.Tags.ClearFlags(AbilityTag.CanJump);
      SimpleAbilityManager.Stop(this);
    }
  }
}