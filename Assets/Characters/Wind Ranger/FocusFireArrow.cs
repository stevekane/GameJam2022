using UnityEngine;

public class FocusFireArrow : MonoBehaviour {
  public GameObject DestructionPrefab;
  public HitParams HitParams;

  void OnCollisionEnter(Collision c) {
    VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.contacts[0].point);
    Destroy(gameObject);
  }

  void OnProjectileEnter(ProjectileCollision c) {
    if (c.Collider.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(HitParams, transform);
      VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.Point);
      Destroy(gameObject);
    }
  }
}