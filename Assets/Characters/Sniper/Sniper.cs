using UnityEngine;

/*
action[t] ≡
  WHEN inrange(t)
  THEN F[t,aimTim]
  ELSE M[t]

I ≡
  target(t)
  action(t)

M[t] ≡
  EITHER
    clock(dt)
    WHEN inrange(t)
    THEN
      F[t,aimTime]
    ELSE
      moveTowards(dt,t)
      M[t]
  OR
    death(t)
    I

F[t,r] ≡
  WHEN remaining > 0
  THEN
    EITHER
      clock(dt)
      aim(dt,t)
      F[t,r-dt]
    OR
      death(t)
      I
  ELSE
    fire(t)
    wait(recoveryTime)
    Action[t]
*/
public class Sniper : MonoBehaviour {
  public LayerMask TargetLayerMask;
  public float MinDistance;
  public float MaxDistance;
  public float EyeHeight;

  public Transform Target;

  void FixedUpdate() {
    if (!Target) {
      Target = FindObjectOfType<Player>().transform;
    }
    if (Target) {
      var toTarget = Target.position.XZ()-transform.position.XZ();
      var distanceToTarget = toTarget.magnitude;
      var inRange = distanceToTarget >= MinDistance && distanceToTarget <= MaxDistance;
      var eye = transform.position+Vector3.up*EyeHeight;
      var ray = new Ray(eye, toTarget.normalized);
      var didHit = Physics.Raycast(ray, out var hit, distanceToTarget, TargetLayerMask, QueryTriggerInteraction.Collide);
      var hasClearShot = hit.transform && hit.transform.TryGetComponent(out Hurtbox hb) && hb.Defender.transform == Target;
      if (hasClearShot) {
        Debug.DrawRay(ray.origin, ray.direction*hit.distance, Color.green);
        Debug.Log($"Hit hurtbox {hit.transform.name}");
      } else {
        Debug.DrawRay(ray.origin, ray.direction*distanceToTarget, Color.red);
      }
      Debug.Log((inRange, hasClearShot) switch {
        (true, true) => "good to go",
        (true, false) => "safe distance no line of sight",
        (false, true) => "not at safe distance",
        (false, false) => "nothing is right"
      });
    } else {
      Debug.Log("Do not have target");
      // no target... what to do
    }
  }
}