using UnityEngine;

public class SprintAbility : SimpleAbility {
  [SerializeField] BaseMovementSpeed BaseMovementSpeed;
  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] MaxMovementSpeed MaxMovementSpeed;
  [SerializeField] InputManager InputManager;
  [SerializeField] float SprintMovementSpeed;

  public override void OnRun() {
    IsRunning = true;
    MovementSpeed.Value = SprintMovementSpeed;
    MaxMovementSpeed.Value = SprintMovementSpeed;
  }

  public override void OnStop() {
    IsRunning = false;
    MovementSpeed.Value = BaseMovementSpeed.Value;
    MaxMovementSpeed.Value = BaseMovementSpeed.Value;
  }
}