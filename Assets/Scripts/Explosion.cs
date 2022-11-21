using UnityEngine;

public class Explosion : MonoBehaviour {
  float Radius { get => GetComponent<ParticleSystem>().main.startSize.constantMax*.5f; }

  void Start() {
    var numHits = Physics.OverlapSphereNonAlloc(transform.position, Radius, PhysicsQuery.Colliders);
    for (int i = 0; i < numHits; i++) {
      if (PhysicsQuery.Colliders[i].TryGetComponent(out Player player)) {
        //Debug.Log("Player go BOOM");
      }
    }
    Destroy(gameObject, 2f);  // Assume animation is done before 2s.
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Radius);
  }
}
