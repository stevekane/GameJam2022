using UnityEngine;

public class BigFatSlowBoom : MonoBehaviour {
  [SerializeField] GameObject ContactVFX;
  [SerializeField] AudioClip ContactSFX;
  HitParams HitParams;

  public void InitHitParams(HitConfig hitConfig, Attributes attacker) {
    HitParams = new HitParams(hitConfig, attacker.SerializedCopy, attacker.gameObject, gameObject);
  }

  void Detonate(GameObject target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.TryAttack(HitParams.Clone());
    } else {
      SFXManager.Instance.TryPlayOneShot(ContactSFX);
      VFXManager.Instance.TrySpawnEffect(ContactVFX, transform.position);
    }
    Destroy(gameObject);
  }
  void OnCollisionEnter(Collision c) => Detonate(c.transform.gameObject);
  void OnProjectileEnter(ProjectileCollision c) => Detonate(c.Collider.gameObject);
}