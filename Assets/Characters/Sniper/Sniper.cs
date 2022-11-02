using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sniper : MonoBehaviour {
  public PortalAbility PortalAbility;
  public AbilityManager AbilityManager;
  public Status Status;
  public LayerMask TargetLayerMask;
  public Transform Target;
  public float MinDistance;
  public float MaxDistance;
  public float EyeHeight;
  public Bundle Bundle = new();
  public Vector3 Eye { get => Vector3.up*EyeHeight+transform.position; }
  public IEnumerable<Transform> VisibleTargets {
    get {
      var hitCount = Physics.OverlapSphereNonAlloc(
        transform.position,
        MaxDistance,
        PhysicsBuffers.Colliders,
        TargetLayerMask,
        QueryTriggerInteraction.Collide);
      for (var i = 0; i < hitCount; i++) {
        var collider = PhysicsBuffers.Colliders[i];
        var target = TryGetTarget(collider.transform);
        if (target) {
          var toTarget = collider.transform.position.XZ()-transform.position.XZ();
          var distanceToTarget = toTarget.magnitude;
          var ray = new Ray(Eye, toTarget);
          var didHit = Physics.Raycast(
            ray,
            out var hit,
            distanceToTarget,
            TargetLayerMask,
            QueryTriggerInteraction.Collide);
          if (didHit) {
            if (TryGetTarget(hit.transform) == target) {
              yield return target;
            }
          }
        }
      }
    }
  }

  Transform TryGetTarget(Transform t) => t.GetComponent<Hurtbox>()?.Defender.transform;

  IEnumerator BaseBehavior() {
    while (true) {
      Target = null;
      foreach (var target in VisibleTargets) {
        Target = target;
      }
      if (Target) {
        var toTarget = Target.position-transform.position;
        var threat = 1-toTarget.magnitude/MaxDistance;
        var opportunity = Vector3.Dot(transform.forward, toTarget.normalized);
        Debug.Log("Trying to portal");
        PortalAbility.ThreatPosition = Target.position;
        AbilityManager.TryInvoke(PortalAbility.PortalStart);
        yield return Fiber.Until(() => !PortalAbility.IsRunning);
      } else {
        Debug.Log("No visible targets");
      }
      yield return null;
    }
  }

  void Start() => Bundle.StartRoutine(new Fiber(BaseBehavior()));
  void OnDestroy() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}