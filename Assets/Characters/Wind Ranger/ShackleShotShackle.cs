using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ShackleShotShackle : MonoBehaviour {
  public static int MAX_COLLIDERS = 16;
  public static Collider[] Colliders = new Collider[MAX_COLLIDERS];

  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public float Radius;
  public float Radians;

  void OnProjectileEnter(ProjectileCollision c) {
    var velocity = GetComponent<Rigidbody>().velocity;
    var direction = velocity.normalized;
    var hits = Physics.OverlapSphereNonAlloc(c.Collider.transform.position, Radius, Colliders, LayerMask, TriggerInteraction);
    var origin = c.Collider.bounds.center;
    for (var i = 0; i < hits; i++) {
      if (Colliders[i] != c.Collider) {
        var dest = Colliders[i].bounds.center;
        Debug.DrawLine(origin, dest, Color.red, 5);
      }
    }
    // TODO: check for radians criteria (both pitch and yaw)
    // TODO: Probably should not destroy it.
    // Seems like it should apply an effect to valid targets
    // then release the effect and die after n seconds
    Destroy(gameObject);
  }

  void OnCollisionEnter(Collision c) {
    Destroy(gameObject);
  }
}