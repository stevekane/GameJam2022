using UnityEngine;

public class DoubleJumpAbility : SimpleAbility {
  public AbilityAction Jump;

  [Header("Reads From")]
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpStrength;
  [Header("Writes To")]
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Animator Animator;

  void Awake() {
    Jump.Listen(Main);
  }

  void Main() {
    SimpleAbilityManager.RemoveTag(AbilityTag.CanJump);
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    PhysicsMotion.OverrideVelocityY(JumpStrength, 1);
  }
}