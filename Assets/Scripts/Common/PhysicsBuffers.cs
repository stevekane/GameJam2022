using UnityEngine;

public static class PhysicsBuffers {
  public static int MAX_COLLIDERS = 256;
  public static int MAX_RAYCAST_HITS = 256;
  public static Collider[] Colliders = new Collider[MAX_COLLIDERS];
  public static RaycastHit[] RaycastHits = new RaycastHit[MAX_RAYCAST_HITS];

  public static bool InFieldOfView(
  Vector3 position,
  Vector3 forward,
  Vector3 target,
  float fieldOfView) {
    var angle = Vector3.Angle(forward, target - position);
    var halfFOV = fieldOfView/2;
    return -angle >= -halfFOV || angle <= halfFOV;
  }

  public static bool IsVisibleFrom(
  Vector3 position,
  Transform target,
  LayerMask layerMask,
  QueryTriggerInteraction triggerInteraction) {
    var delta = target.position - position;
    var direction = delta.normalized;
    var distance = delta.magnitude;
    var didHit = Physics.Raycast(
      position,
      direction,
      out RaycastHit hit,
      distance,
      layerMask,
      triggerInteraction);
    return didHit && hit.transform == target.transform;
  }

  public static int VisibleTargets(
  Vector3 position,
  Vector3 forward,
  float fieldOfView,
  float maxDistance,
  LayerMask targetLayerMask,
  QueryTriggerInteraction targetQueryTriggerInteraction,
  LayerMask visibleTargetLayerMask,
  QueryTriggerInteraction visibleQueryTriggerInteraction,
  Collider[] buffer) {
    var hitCount = Physics.OverlapSphereNonAlloc(
      position,
      maxDistance,
      buffer,
      targetLayerMask,
      targetQueryTriggerInteraction);
    var i = 0;
    while (i < hitCount) {
      var candidate = buffer[i];
      var isInFieldOfView = InFieldOfView(
        position: position,
        forward: forward,
        target: candidate.transform.position,
        fieldOfView: fieldOfView);
      var isVisible = IsVisibleFrom(
        position: position,
        target: candidate.transform,
        layerMask: visibleTargetLayerMask,
        triggerInteraction: visibleQueryTriggerInteraction);
      if (isInFieldOfView && isVisible) {
        i++;
      } else {
        var tmp = candidate;
        buffer[i] = buffer[hitCount-1];
        buffer[hitCount-1] = tmp;
        hitCount--;
      }
    }
    return hitCount;
  }
}