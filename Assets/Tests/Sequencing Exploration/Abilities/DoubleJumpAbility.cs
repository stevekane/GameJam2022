using UnityEngine;

public class DoubleJumpAbility : SimpleAbility {
  [SerializeField] AudioClip SFX;
  [SerializeField] Animator Animator;
  [SerializeField] Velocity Velocity;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] float JumpStrength;

  public override void OnRun() {
    SimpleAbilityManager.Tags.Current.ClearFlags(AbilityTag.CanJump);
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    Velocity.Value.y = JumpStrength;
  }
}