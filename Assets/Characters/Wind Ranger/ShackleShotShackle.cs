using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ShackleShotShackle : MonoBehaviour {
  public ShackleShotBinding BindingPrefab;
  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public float Radius;
  public float Radians;

  bool TryGetStatus(GameObject g, out Status status) => status = g.GetComponent<Hurtbox>()?.Defender.GetComponent<Status>();
  void Stop() => Destroy(gameObject);
  void OnDestroy() => Stop();
  void OnCollisionEnter(Collision c) => Stop();
  void OnProjectileEnter(ProjectileCollision c) {
    if (TryGetStatus(c.Collider.gameObject, out Status targetStatus)) {
      var rigidBody = GetComponent<Rigidbody>();
      var direction = rigidBody.velocity.normalized;
      var colliders = PhysicsBuffers.Colliders;
      var hits = Physics.OverlapSphereNonAlloc(c.Collider.transform.position, Radius, colliders, LayerMask, TriggerInteraction);
      var origin = c.Collider.bounds.center;
      Status bestStatus = null;
      float bestScore = 0;
      for (var i = 0; i < hits; i++) {
        if (colliders[i] != c.Collider && TryGetStatus(colliders[i].gameObject, out Status candidateStatus)) {
          var dest = colliders[i].bounds.center;
          var delta = dest-origin;
          var toDest = delta.normalized;
          var angleScore = Vector3.Dot(direction, toDest);
          var distanceScore = 1-delta.magnitude/Radius;
          var score = angleScore+distanceScore;
          if (score > bestScore) {
            bestScore = score;
            bestStatus = candidateStatus;
          }
        }
      }
      if (bestStatus) {
        var halfway = targetStatus.transform.position+(bestStatus.transform.position-targetStatus.transform.position)/2;
        var binding = Instantiate(BindingPrefab, halfway, Quaternion.identity);
        binding.First = targetStatus;
        binding.Second = bestStatus;
      }
    }
    Stop();
  }
}