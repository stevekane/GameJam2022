using UnityEngine;

public class DoubleJumpAbility : SimpleAbility {
  public AbilityAction Jump;

  [Header("Reads From")]
  [SerializeField] Gravity Gravity;
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpHeight;
  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Animator Animator;

  void Awake() {
    Jump.Listen(Main);
  }

  void Main() {
    AbilityManager.RemoveTag(AbilityTag.CanJump);
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    CharacterController.PhysicsVelocity.y = Mathf.Sqrt(2 * Mathf.Abs(Gravity.RisingStrength) * JumpHeight);
  }
}