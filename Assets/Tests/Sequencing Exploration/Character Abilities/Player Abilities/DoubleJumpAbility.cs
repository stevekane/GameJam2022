using UnityEngine;

public class DoubleJumpAbility : SimpleAbility {
  public AbilityAction Jump;

  [Header("Reads From")]
  [SerializeField] Gravity Gravity;
  [SerializeField] AnimationClip AnimationClip;
  [SerializeField] AudioClip SFX;
  [SerializeField] float JumpHeight;
  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] AnimatorGraph AnimatorGraph;
  [SerializeField] HitStop HitStop;

  void Awake() {
    Jump.Listen(Main);
  }

  void Main() {
    AbilityManager.RemoveTag(AbilityTag.CanJump);
    var v = CharacterController.PhysicsVelocity;
    v.y = Mathf.Sqrt(2 * Mathf.Abs(Gravity.RisingStrength) * JumpHeight);
    CharacterController.SetPhysicsVelocity(v);
    HitStop.TicksRemaining = 0;
    AnimatorGraph.Play(AnimationClip);
    AudioSource.PlayOneShot(SFX);
  }
}