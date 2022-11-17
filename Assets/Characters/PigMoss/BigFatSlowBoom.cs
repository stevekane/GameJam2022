using UnityEngine;

public class BigFatSlowBoom : MonoBehaviour {
  public GameObject VFXPrefab;
  public AudioClip SFXClip;
  public float Damage;
  public float KnockbackStrength;
  public Timeval HitStopDuration;
  void Detonate(GameObject target) {
    if (target.TryGetComponent(out Hurtbox hurtbox)) {
      var hitParams = new HitParams {
        Damage = Damage,
        KnockbackType = KnockBackType.Delta,
        KnockbackStrength = KnockbackStrength,
        HitStopDuration = HitStopDuration,
        SFX = SFXClip,
        VFX = VFXPrefab,
      };
      hurtbox.Defender.OnHit(hitParams, transform);
    } else {
      SFXManager.Instance.TryPlayOneShot(SFXClip);
      VFXManager.Instance.TrySpawnEffect(VFXPrefab, transform.position);
    }
    Destroy(gameObject);
  }
  void OnCollisionEnter(Collision c) => Detonate(c.transform.gameObject);
  void OnProjectileEnter(ProjectileCollision c) => Detonate(c.Collider.gameObject);
}