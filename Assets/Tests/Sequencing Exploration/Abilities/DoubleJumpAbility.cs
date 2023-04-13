using UnityEngine;

public class DoubleJumpAbility : SimpleAbility {
  [Header("Reads From")]
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpStrength;
  [Header("Writes To")]
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Animator Animator;

  public override void OnRun() {
    SimpleAbilityManager.Tags.Current.ClearFlags(AbilityTag.CanJump);
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    PhysicsMotion.OverrideVelocityY(JumpStrength, 1);
  }
}