using UnityEngine;

public class FocusFireArrow : MonoBehaviour {
  public GameObject DestructionPrefab;
  public float Damage = 1;

  void OnCollisionEnter(Collision c) {
    VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.contacts[0].point);
    Destroy(gameObject);
  }

  void OnProjectileEnter(ProjectileCollision c) {
    if (c.Collider.TryGetComponent(out Hurtbox hurtbox)) {
      var hitParams = new HitParams {
        HitStopDuration = Timeval.FromMillis(50),
        Damage = Damage,
        KnockbackStrength = 1,
        KnockbackType = KnockBackType.Forward
      };
      hurtbox.Defender.OnHit(hitParams, transform);
      VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.Point);
      Destroy(gameObject);
    }
  }
}