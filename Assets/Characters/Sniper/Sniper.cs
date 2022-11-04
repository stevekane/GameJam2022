using System;
using System.Collections;
using UnityEngine;

public class ChooseRandomDirection : IStoppableValue<Vector3> {
  public object Current { get => Value; }
  public void Reset() {
    IsRunning = true;
    Value = default;
  }
  public bool MoveNext() {
    Value = UnityEngine.Random.insideUnitSphere.XZ().normalized;
    IsRunning = false;
    return IsRunning;
  }
  public void Stop() => IsRunning = false;
  public bool IsRunning { get; internal set; } = true;
  public Vector3 Value { get; internal set; }
}

public class SampleForSafestDirection : IStoppableValue<Vector3> {
  public LayerMask EnvironmentLayerMask;
  public Vector3 Position;
  public Vector3 InitialDirection;
  public float ProjectileSpeed;
  public float EstimatedTravelTime;
  public float EstimatedGravity;
  public int DirectionSamples;
  public object Current { get => Value; }
  public bool MoveNext() {
    IsRunning = false;
    Value = SafestDirection();
    return false;
  }
  public void Reset() => IsRunning = true;
  public void Stop() => IsRunning = false;
  public bool IsRunning { get; set; } = true;
  public Vector3 Value { get; internal set; }

  float LikelyDistance(Vector3 direction) {
    var maxDistance = EstimatedTravelTime*ProjectileSpeed;
    var didHit = Physics.Raycast(
      Position,
      direction,
      out var hit,
      maxDistance,
      EnvironmentLayerMask);
    var finalPosition = didHit ? hit.point : maxDistance*direction;
    Debug.DrawRay(Position, finalPosition, Color.grey, 2f);
    return Vector3.Distance(Position, finalPosition);
  }

  Vector3 SafestDirection() {
    var bestDistance = 0f;
    var bestDirection = InitialDirection;
    var degreesPerSample = 360/DirectionSamples;
    for (var i = 0; i < DirectionSamples; i++) {
      var rotation = Quaternion.Euler(0, degreesPerSample*i, 0);
      var direction = rotation*InitialDirection;
      var distance = LikelyDistance(direction);
      if (distance > bestDistance) {
        bestDistance = distance;
        bestDirection = direction;
      }
    }
    return bestDirection;
  }
}

public class Sniper : MonoBehaviour {
  public PortalAbility PortalAbility;
  public PowerShot PowerShotAbility;
  public AbilityManager AbilityManager;
  public Status Status;
  public Attributes Attributes;
  public Mover Mover;
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
        if (toTarget.magnitude < MinDistance) {
          PortalAbility.GetPortalDirection = new SampleForSafestDirection {
            EnvironmentLayerMask = EnvironmentLayerMask,
            Position = transform.position,
            InitialDirection = transform.forward,
            ProjectileSpeed = PortalAbility.PortalPrefab.GetComponent<Projectile>().InitialSpeed,
            EstimatedTravelTime = PortalAbility.WaitDuration.Seconds,
            EstimatedGravity = GetComponent<Mover>().Gravity,
            DirectionSamples = 8
          };
          AbilityManager.TryInvoke(PortalAbility.PortalStart);
          yield return Fiber.Until(() => !PortalAbility.IsRunning);
        } else {
          Mover.GetAxes(AbilityManager, out var desiredMove, out var desiredFacing);
          Mover.UpdateAxes(AbilityManager, desiredMove, toTarget.normalized);
          var aimingTimeout = Fiber.Wait(Timeval.FramesPerSecond*1);
          var aimed = Fiber.Until(() => Vector3.Dot(transform.forward, toTarget.normalized) >= .98f);
          yield return Fiber.Any(aimingTimeout, aimed);
          AbilityManager.TryInvoke(PowerShotAbility.MakeRoutine);
          yield return Fiber.Until(() => !PowerShotAbility.IsRunning);
        }
      } else {
        yield return null;
      }
    }
  }

  void Start() => Bundle.StartRoutine(new Fiber(BaseBehavior()));
  void OnDestroy() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}