using UnityEngine;

public class JumpAbility : SimpleAbility {
  [SerializeField] AudioClip SFX;
  [SerializeField] Animator Animator;
  [SerializeField] Velocity Velocity;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] float JumpStrength;

  public override void OnRun() {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    Velocity.Value.y = JumpStrength;
    Debug.Log($"{FixedFrame.Instance.Tick} Jump");
  }
}