using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PowerShotArrow : MonoBehaviour {
  [SerializeField] Attributes Attributes;
  [SerializeField] HitConfig HitConfig;
  public GameObject DestructionPrefab;
  public float DamageReductionPerTarget = .2f;

  int TargetsHit;

  void OnCollisionEnter(Collision c) {
    VFXManager.Instance.TrySpawnEffect(DestructionPrefab, c.contacts[0].point);
    Destroy(gameObject);
  }

  void OnProjectileEnter(ProjectileCollision c) {
    if (c.Collider.TryGetComponent(out Hurtbox hurtbox)) {
      var scaling = Mathf.Pow(1-DamageReductionPerTarget, TargetsHit);
      var hitConfig = HitConfig.Scale(HitConfig, scaling);
      TargetsHit++;
      hurtbox.TryAttack(Attributes, hitConfig);
    }
  }
}