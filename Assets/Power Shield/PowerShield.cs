using UnityEngine;

public class PowerShield : MonoBehaviour {
  [SerializeField] Damage Damage;
  [SerializeField] Animator Animator;
  [SerializeField] float MaxDamage;
  [SerializeField] AudioClip OnHurtSFX;
  [SerializeField] GameObject OnHurtVFX;

  float Condition => 1 - (Mathf.Min(Damage.Points, MaxDamage) / MaxDamage);

  void OnHurt() {
    Animator.SetTrigger("OnHurt");
    SFXManager.Instance.TryPlayOneShot(OnHurtSFX);
    VFXManager.Instance.TrySpawnEffect(OnHurtVFX, transform.position);
  }

  void FixedUpdate() {
    Animator.SetFloat("Condition", Condition);
  }
}