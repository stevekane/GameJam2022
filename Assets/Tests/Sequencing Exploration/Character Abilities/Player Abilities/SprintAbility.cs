using UnityEngine;

public class SprintAbility : SimpleAbility {
  public AbilityAction StartSprinting;
  public AbilityAction StopSprinting;

  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] float MoveSpeedBonus = 6;

  void Awake() {
    StartSprinting.Listen(StartSprint);
    StopSprinting.Listen(Stop);
  }

  void StartSprint() {
    IsRunning = true;
  }

  void FixedUpdate() {
    if (IsRunning) {
      MovementSpeed.Add(MoveSpeedBonus);
    }
  }
}