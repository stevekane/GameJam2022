using UnityEngine;
using UnityEngine.Serialization;

public class Effects : MonoBehaviour {
  [SerializeField] Vector3 VFXOffset;

  [Header("OnHurt")]
  [FormerlySerializedAs("VFX")]
  [SerializeField] GameObject OnHurtVFX;
  [FormerlySerializedAs("Clip")]
  [SerializeField] AudioClip OnHurtSFX;
  [Header("OnHit")]
  [SerializeField] GameObject OnHitVFX;
  [SerializeField] AudioClip OnHitSFX;

  [SerializeField] GameObject OnParryVFX;
  [SerializeField] AudioClip OnParrySFX;

  [SerializeField] GameObject OnDeathVFX;
  [SerializeField] AudioClip OnDeathSFX;

  [SerializeField] Renderer Renderer;
  Status Status;

  [SerializeField] DynamicTrailMeshRenderer WeaponTrailRenderer;

  void OnAttackStart() {
    if (WeaponTrailRenderer != null) {
      WeaponTrailRenderer.Emitting = true;
    }
  }

  void OnAttackEnd() {
    if (WeaponTrailRenderer != null) {
      WeaponTrailRenderer.Emitting = false;
    }
  }

  void OnDeath(Vector3 normal) {
    var position = transform.position+transform.TransformVector(VFXOffset);
    var rotation = Quaternion.LookRotation(normal.normalized);
    var obj = VFXManager.Instance.TrySpawnEffect(OnDeathVFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(OnDeathSFX);
  }

  void OnHurt(HitParams hitParams) {
    var position = transform.position+transform.TransformVector(VFXOffset);
    var direction = hitParams.KnockbackVector;
    var rotation = Quaternion.LookRotation(direction.normalized);
    VFXManager.Instance.TrySpawnEffect(OnHurtVFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(OnHurtSFX);
  }

  void OnHit(HitParams hitParams) {
    var position = hitParams.Defender.transform.position+transform.TransformVector(VFXOffset);
    var direction = hitParams.KnockbackVector;
    var rotation = Quaternion.LookRotation(direction.normalized);
    VFXManager.Instance.TrySpawnEffect(OnHitVFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(OnHitSFX);
  }

  void OnWasParried(HitParams hitParams) {
    var position = transform.position+transform.TransformVector(VFXOffset);
    var direction = -hitParams.KnockbackVector;
    var rotation = Quaternion.LookRotation(direction.normalized);
    VFXManager.Instance.TrySpawnEffect(OnParryVFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(OnParrySFX);
  }

  void Awake() {
    this.InitComponent(out Status, optional:true);
  }
  void FixedUpdate() {
    if (Renderer && Status)
      Renderer.enabled = Status.IsHittable;
  }
}