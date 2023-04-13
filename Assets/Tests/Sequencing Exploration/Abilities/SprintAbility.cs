using UnityEngine;

public class SprintAbility : SimpleAbility {
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] float SprintMovementSpeed;

  public override void OnRun() {
    IsRunning = true;
  }

  void FixedUpdate() {
    MovementSpeed.Value = IsRunning ? SprintMovementSpeed : MovementSpeed.Base;
  }

  public override void OnStop() {
    IsRunning = false;
  }
}