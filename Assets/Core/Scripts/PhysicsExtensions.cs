using UnityEngine;

public static class PhysicsExtensions {
  public static bool CapsuleColliderCast(
  this CapsuleCollider capsuleCollider,
  Vector3 position,
  Vector3 direction,
  float maxDistance,
  out RaycastHit hit,
  LayerMask layerMask = default,
  QueryTriggerInteraction queryTriggerInteraction = default) {
    float radius = capsuleCollider.radius;
    Vector3 point1;
    Vector3 point2;
    switch (capsuleCollider.direction) {
      case 0: // X-axis
        point1 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.left * (capsuleCollider.height / 2 - capsuleCollider.radius));
        point2 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.right * (capsuleCollider.height / 2 - capsuleCollider.radius));
        break;
      case 1: // Y-axis
        point1 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.down * (capsuleCollider.height / 2 - capsuleCollider.radius));
        point2 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius));
        break;
      case 2: // Z-axis
        point1 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.back * (capsuleCollider.height / 2 - capsuleCollider.radius));
        point2 = capsuleCollider.transform.TransformPoint(capsuleCollider.center + Vector3.forward * (capsuleCollider.height / 2 - capsuleCollider.radius));
        break;
      default:
        throw new System.NotImplementedException("Unknown capsule direction!");
    }
    return Physics.CapsuleCast(point1, point2, radius, direction, out hit, maxDistance, layerMask, queryTriggerInteraction);
  }
}