using UnityEngine;

public class JumpAbility : SimpleAbility {
  [SerializeField] AudioClip SFX;
  [SerializeField] Animator Animator;
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] HitStop HitStop;
  [SerializeField] float JumpStrength;

  public override void OnRun() {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    PhysicsMotion.OverrideVelocityY(JumpStrength, 1);
    HitStop.TicksRemaining = 0;
  }
}