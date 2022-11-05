using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PowerShotArrow : MonoBehaviour {
  public GameObject DestructionPrefab;
  public float DamageReductionPerTarget = .2f;
  public float Damage = 100;
  public HitParams HitParams;

  int TargetsHit;

  void OnCollisionEnter(Collision c) {
    VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.contacts[0].point);
    Destroy(gameObject);
  }

  void OnProjectileEnter(ProjectileCollision c) {
    if (c.Collider.TryGetComponent(out Hurtbox hurtbox)) {
      HitParams.Damage *= Mathf.Pow(1-DamageReductionPerTarget, TargetsHit);
      TargetsHit++;
      hurtbox.Defender.OnHit(HitParams, transform);
    }
  }
}