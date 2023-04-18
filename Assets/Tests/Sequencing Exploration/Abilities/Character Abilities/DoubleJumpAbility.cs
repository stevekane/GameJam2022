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
    Jump.Ability = this;
    Jump.CanRun = true;
    Jump.Source.Listen(Main);
  }

  void Main() {
    Debug.LogWarning("Double jump previously clears AbilityTag.CanJump");
    // SimpleAbilityManager.Tags.Current.ClearFlags(AbilityTag.CanJump);
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    PhysicsMotion.OverrideVelocityY(JumpStrength, 1);
  }
}