using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class PhysicsQueryConfig {
  public LayerMask LayerMask = new();
  public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;
}

[Serializable]
public class TargetingConfig {
  public float FieldOfView;
  public float MaxDistance;
  public Vector3 EyeOffset;
  public PhysicsQueryConfig TargetQueryConfig;
  public PhysicsQueryConfig EnvironmentQueryConfig;
}

[Serializable]
public class AcquireTargets : IEnumerator {
  public int Value { get; private set; }
  public object Current => Value;
  public TargetingConfig Config;
  public Transform Owner;
  public Collider[] Colliders;
  public AcquireTargets(Transform owner, TargetingConfig config, Collider[] colliders) {
    Owner = owner;
    Config = config;
    Colliders = colliders;
  }
  public void Reset() => Value = 0;
  public bool MoveNext() {
    Value = PhysicsQuery.VisibleTargets(
      position: Owner.position+Config.EyeOffset,
      forward: Owner.forward,
      fieldOfView: Config.FieldOfView,
      maxDistance: Config.MaxDistance,
      targetLayerMask: Config.TargetQueryConfig.LayerMask,
      targetQueryTriggerInteraction: Config.TargetQueryConfig.TriggerInteraction,
      visibleTargetLayerMask: Config.TargetQueryConfig.LayerMask | Config.EnvironmentQueryConfig.LayerMask,
      visibleQueryTriggerInteraction: Config.EnvironmentQueryConfig.TriggerInteraction,
      buffer: Colliders);
    return Value == 0;
  }
}

public static class PhysicsQuery {
  public static int MAX_COLLIDERS = 256;
  public static int MAX_RAYCAST_HITS = 256;
  public static Collider[] Colliders = new Collider[MAX_COLLIDERS];
  public static RaycastHit[] RaycastHits = new RaycastHit[MAX_RAYCAST_HITS];

  public static bool GroundCheck(
  Vector3 origin,
  float maxDistance = .2f) {
    return Physics.Raycast(origin, Vector3.down, maxDistance, Defaults.Instance.EnvironmentLayerMask);
  }

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