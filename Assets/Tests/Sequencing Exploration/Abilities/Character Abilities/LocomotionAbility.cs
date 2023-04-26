using UnityEngine;

public class LocomotionAbility : SimpleAbility {
  [Header("Reads From")]
  [SerializeField] MovementSpeed MovementSpeed;
  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] Animator Animator;

  public AbilityAction<Vector3> Move;

  void Awake() {
    Move.Listen(Main);
  }

  void Main(Vector3 value) {
    if (value.sqrMagnitude > 0) {
      AbilityManager.transform.rotation = Quaternion.LookRotation(value);
    }
    var velocity = MovementSpeed.Value * value;
    CharacterController.Move(Time.fixedDeltaTime * velocity);
    Animator.SetFloat("Speed", Mathf.Round(velocity.magnitude));
  }
}