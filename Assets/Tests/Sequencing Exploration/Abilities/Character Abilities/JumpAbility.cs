using UnityEngine;

public class JumpAbility : SimpleAbility {
  public AbilityAction Jump;

  [Header("Reads From")]
  [SerializeField] Gravity Gravity;
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpHeight;
  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Animator Animator;
  [SerializeField] HitStop HitStop;

  void Awake() {
    Jump.Listen(Main);
  }

  void Main() {
    Animator.SetTrigger("Jump");
    AudioSource.PlayOneShot(SFX);
    CharacterController.KinematicCharacterMotor.ForceUnground();
    var v = CharacterController.PhysicsVelocity;
    v.y = Mathf.Sqrt(2 * Mathf.Abs(Gravity.RisingStrength) * JumpHeight);
    CharacterController.SetPhysicsVelocity(v);
    HitStop.TicksRemaining = 0;
  }
}