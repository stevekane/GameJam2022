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
}