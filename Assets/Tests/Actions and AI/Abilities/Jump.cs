using UnityEngine;

namespace ActionsAndAI {
  public class Jump : SimpleAbility {
    [SerializeField] Velocity Velocity;
    [SerializeField] float Strength = 15;

    public override void OnRun() {
      Velocity.Value.y = Strength;
      SimpleAbilityManager.Stop(this);
    }
  }
}