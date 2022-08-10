using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PowerShotArrow : MonoBehaviour {
  public GameObject DestructionPrefab;
  public float DamageReductionPerTarget = .2f;
  public float Damage = 100;

  int TargetsHit;

  void OnCollisionEnter(Collision c) {
    if (c.collider.TryGetComponent(out Hurtbox hurtbox)) {
      if (hurtbox.Defender.TryGetComponent(out Damage damage)) {
        damage.AddPoints(Damage*Mathf.Pow(1-DamageReductionPerTarget, TargetsHit));
        TargetsHit++;
      }
    } else {
      VFXManager.Instance.TrySpawnEffect(DestructionPrefab, transform.position);
      Destroy(gameObject);
    }
  }
}