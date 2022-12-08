using UnityEngine;

public class FocusFireArrow : MonoBehaviour {
  [SerializeField] Attributes Attributes;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] GameObject DestructionPrefab;

  void OnCollisionEnter(Collision c) {
    VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.contacts[0].point);
    Destroy(gameObject);
  }

  void OnProjectileEnter(ProjectileCollision c) {
    if (c.Collider.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject, gameObject));
      VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.Point);
      Destroy(gameObject);
    }
  }
}