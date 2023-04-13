using UnityEngine;

public class LocomotionAbility : SimpleAbilityVector3 {
  [Header("Reads From")]
  [SerializeField] MovementSpeed MovementSpeed;
  [Header("Writes To")]
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] Animator Animator;

  public override void OnRun() {
    if (Value.sqrMagnitude > 0) {
      SimpleAbilityManager.transform.rotation = Quaternion.LookRotation(Value);
    }
    var velocity = MovementSpeed.Value * Value.normalized;
    var motion = Time.deltaTime * velocity;
    DirectMotion.IsActive(true, 0);
    DirectMotion.AddMotion(motion);
    Animator.SetFloat("Speed", Mathf.Round(velocity.magnitude));
  }
}