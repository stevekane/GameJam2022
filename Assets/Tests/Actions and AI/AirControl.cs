using UnityEngine;

namespace ActionsAndAI {
  public class AirControl : SimpleAbilityVector3 {
    public override void OnRun() {
      if (Value.magnitude > 0)
        SimpleAbilityManager.transform.forward = Value;
      Stop();
    }
  }
}