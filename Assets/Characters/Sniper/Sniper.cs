using System.Collections;
using UnityEngine;

public class Sniper : MonoBehaviour {
  public PortalAbility PortalAbility;
  public AbilityManager AbilityManager;
  public Status Status;
  public LayerMask TargetLayerMask;
  public LayerMask EnvironmentLayerMask;
  public Transform Target;
  public float MinDistance;
  public float MaxDistance;
  public float EyeHeight;
  [Tooltip("Degrees")]
  public float FieldOfView;
  public Bundle Bundle = new();

  Transform TryGetTarget(Transform t) => t.GetComponent<Hurtbox>()?.Defender.transform;

  IEnumerator BaseBehavior() {
    while (true) {
      var visibleTargetCount = PhysicsBuffers.VisibleTargets(
        position: transform.position+EyeHeight*Vector3.up,
        forward: transform.forward,
        fieldOfView: 180,
        maxDistance: MaxDistance,
        targetLayerMask: TargetLayerMask,
        targetQueryTriggerInteraction: QueryTriggerInteraction.Collide,
        visibleTargetLayerMask: TargetLayerMask | EnvironmentLayerMask,
        visibleQueryTriggerInteraction: QueryTriggerInteraction.Collide,
        buffer: PhysicsBuffers.Colliders);
      Target = null;
      for (var i = 0; i < visibleTargetCount; i++) {
        Target = TryGetTarget(PhysicsBuffers.Colliders[i].transform);
      }
      if (Target) {
        var toTarget = Target.position-transform.position;
        var threat = 1-toTarget.magnitude/MaxDistance;
        var opportunity = Vector3.Dot(transform.forward, toTarget.normalized);
        PortalAbility.ThreatPosition = Target.position;
        AbilityManager.TryInvoke(PortalAbility.PortalStart);
        yield return Fiber.Until(() => !PortalAbility.IsRunning);
      } else {
        yield return null;
      }
    }
  }

  void Start() => Bundle.StartRoutine(new Fiber(BaseBehavior()));
  void OnDestroy() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}