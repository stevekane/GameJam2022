using UnityEngine;

public class HurtEffects : MonoBehaviour {
  [SerializeField] Vector3 VFXOffset;
  [SerializeField] GameObject VFX;
  [SerializeField] AudioClip Clip;

  void OnDamage(DamageInfo info) {
    var position = transform.position+transform.TransformVector(VFXOffset);
    var delta = (transform.position-info.Attacker.position).XZ();
    var rotation = Quaternion.LookRotation(delta.normalized);
    VFXManager.Instance.TrySpawnEffect(VFX, position, rotation);
    SFXManager.Instance.TryPlayOneShot(Clip);
  }
}