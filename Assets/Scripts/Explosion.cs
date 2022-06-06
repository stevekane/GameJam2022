using UnityEngine;

public class Explosion : MonoBehaviour {
  float DamageRadius { get { return GetComponent<ParticleSystem>().main.startSize.constantMax*.5f; } }

  Collider[] hits = new Collider[32];
  void Start() {
    Destroy(gameObject, 2f);  // Assume animation is done before 2s.
    var numHits = Physics.OverlapSphereNonAlloc(transform.position, DamageRadius, hits);
    for (int i = 0; i < numHits; i++) {
      if (hits[i].TryGetComponent(out Player player)) {
        Debug.Log("Player go BOOM");
      } else if (hits[i].TryGetComponent(out Hittable hit)) {
        hit.ExplosionHitMe(this);
      }
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, DamageRadius);
  }
}
