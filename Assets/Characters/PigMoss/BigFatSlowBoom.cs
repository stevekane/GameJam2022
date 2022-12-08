using UnityEngine;

public class BigFatSlowBoom : MonoBehaviour {
  [SerializeField] Attributes Attributes;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] GameObject ContactVFX;
  [SerializeField] AudioClip ContactSFX;

  void Detonate(GameObject target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject, gameObject));
    } else {
      SFXManager.Instance.TryPlayOneShot(ContactSFX);
      VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    }
    Destroy(gameObject);
  }
  void OnCollisionEnter(Collision c) => Detonate(c.transform.gameObject);
  void OnProjectileEnter(ProjectileCollision c) => Detonate(c.Collider.gameObject);
}