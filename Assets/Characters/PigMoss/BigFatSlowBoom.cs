using UnityEngine;

public class BigFatSlowBoom : MonoBehaviour {
  public HitConfig HitParams;
  public GameObject ContactVFX;
  public AudioClip ContactSFX;
  void Detonate(GameObject target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(HitParams.ComputeParamsDontUse(), transform);
    } else {
      SFXManager.Instance.TryPlayOneShot(ContactSFX);
      VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    }
    Destroy(gameObject);
  }
  void OnCollisionEnter(Collision c) => Detonate(c.transform.gameObject);
  void OnProjectileEnter(ProjectileCollision c) => Detonate(c.Collider.gameObject);
}