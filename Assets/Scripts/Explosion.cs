using UnityEngine;

public class Explosion : MonoBehaviour {
  public LayerMask LayerMask;
  public float Radius = 1;
  public HitParams HitParams;

  void Start() {
    var numHits = Physics.OverlapSphereNonAlloc(transform.position, Radius, PhysicsQuery.Colliders, LayerMask);
    for (int i = 0; i < numHits; i++) {
      if (PhysicsQuery.Colliders[i].TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.Defender?.OnHit(HitParams, transform);
      }
    }
  }

  public void OnDrawGizmos() {
    Gizmos.color = UnityEngine.Color.red;
    Gizmos.DrawWireSphere(transform.position, Radius);
  }
}