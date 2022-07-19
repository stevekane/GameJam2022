using UnityEngine;

/*
Stores a volatile reference handle to a target.
Every frame, it derefernces this volatile handle to see if the target game object
still exists. 

If its target still exists, it aims at it.
Otherwise, it creates a new Volatile target (which is destroyed after 3 seconds)
and stores the volatile handle pointing at this new target.
*/
public class VolatileReferenceTester : MonoBehaviour {
  public GameObject TargetPrefab;
  public Volatile.Handle TargetHandle;
  [Range(1,10)] public float TargetDistance = 10f;
  [Range(1,90)] public float DegreesPerSecond = 15;

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var targetRef = new Volatile.Dereference(TargetHandle);
    if (targetRef.GameObject) {
      var targetPosition = targetRef.GameObject.transform.position;
      var delta = targetPosition-transform.position;
      var direction = delta.normalized;
      var lookAt = Quaternion.LookRotation(direction);
      transform.rotation = Quaternion.RotateTowards(transform.rotation, lookAt, dt*DegreesPerSecond);
    } else {
      var newTarget = Instantiate(TargetPrefab, TargetDistance*Random.onUnitSphere, Quaternion.identity);
      Destroy(newTarget, 3); // Bad destroy unknown to VolatileRefManager ... still handled correctly
      TargetHandle = new Volatile.Handle(newTarget.gameObject);
    }
  }
}