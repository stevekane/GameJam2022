using UnityEngine;

public class BigFatSlowBoom : MonoBehaviour {
  public HitParams HitParams;
  public GameObject ContactVFX;
  public AudioClip ContactSFX;
  void Detonate(GameObject target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(HitParams, transform);
    } else {
      SFXManager.Instance.TryPlayOneShot(ContactSFX);
      VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    }
    Destroy(gameObject);
  }
  void OnCollisionEnter(Collision c) => Detonate(c.transform.gameObject);
  void OnProjectileEnter(ProjectileCollision c) => Detonate(c.Collider.gameObject);
}