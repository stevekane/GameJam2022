using UnityEngine;

public class Explosion : MonoBehaviour {
  [SerializeField] Attributes Attributes;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] LayerMask LayerMask;
  [SerializeField] float Radius = 1;

  void Start() {
    var numHits = Physics.OverlapSphereNonAlloc(transform.position, Radius, PhysicsQuery.Colliders, LayerMask);
    for (int i = 0; i < numHits; i++) {
      if (PhysicsQuery.Colliders[i].TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject, gameObject));
      }
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Radius);
  }
}