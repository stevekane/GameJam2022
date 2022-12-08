using UnityEngine;

public class Explosion : MonoBehaviour {
  [SerializeField] LayerMask LayerMask;
  [SerializeField] float Radius = 1;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Attributes Attributes;

  void Start() {
    var numHits = Physics.OverlapSphereNonAlloc(transform.position, Radius, PhysicsQuery.Colliders, LayerMask);
    for (int i = 0; i < numHits; i++) {
      if (PhysicsQuery.Colliders[i].TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(Attributes, HitConfig);
      }
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Radius);
  }
}