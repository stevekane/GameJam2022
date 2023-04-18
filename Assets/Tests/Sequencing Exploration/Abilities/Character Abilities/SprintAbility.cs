using UnityEngine;

public class SprintAbility : SimpleAbility {
  public AbilityAction Sprint;

  [SerializeField] MovementSpeed MovementSpeed;
  [SerializeField] float MoveSpeedBonus = 6;

  void Awake() {
    Sprint.Listen(Main);
  }

  void Main() {
    MovementSpeed.Add(MoveSpeedBonus);
  }
}