using UnityEngine;

public class JumpAbility : SimpleAbility {
  public AbilityAction Jump;

  [SerializeField] AudioClip SFX;
  [SerializeField] Animator Animator;
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] HitStop HitStop;
  [SerializeField] float JumpStrength;

  void Awake() {
    Jump.Ability = this;
    Jump.CanRun = true;
    Jump.Source.Listen(Main);
  }

  void Main() {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    PhysicsMotion.OverrideVelocityY(JumpStrength, 1);
    HitStop.TicksRemaining = 0;
  }
}