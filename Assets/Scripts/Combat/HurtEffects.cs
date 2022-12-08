using UnityEngine;

public class HurtEffects : MonoBehaviour {
  [SerializeField] Vector3 VFXOffset;
  [SerializeField] GameObject VFX;
  [SerializeField] AudioClip Clip;

  void OnHit(HitParams hitParams) {
    var position = transform.position+transform.TransformVector(VFXOffset);
    var rotation = Quaternion.LookRotation(hitParams.KnockbackVector.normalized);
    VFXManager.Instance.TrySpawnEffect(VFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(Clip);
  }
}