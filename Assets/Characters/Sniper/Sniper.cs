using System.Collections;
using UnityEngine;

public class ChooseRandomDirection : IStoppableValue<Vector3> {
  public bool MoveNext() {
    IsRunning = false;
    Value = UnityEngine.Random.insideUnitSphere.XZ().normalized;
    return IsRunning;
  }
  public void Reset() => IsRunning = true;
  public void Stop() => IsRunning = false;
  public object Current { get => Value; }
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
  public bool MoveNext() {
    IsRunning = false;
    Value = SafestDirection();
    return false;
  }
  public void Reset() => IsRunning = true;
  public void Stop() => IsRunning = false;
  public object Current { get => Value; }
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
  public Attributes Attributes;
  public Status Status;
  public Mover Mover;
  public LayerMask TargetLayerMask;
  public LayerMask EnvironmentLayerMask;
  public Transform Target;
  public float MinDistance;
  public float MaxDistance;
  public float EyeHeight;
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
      Target = visibleTargetCount > 0 ? PhysicsBuffers.Colliders[0].transform : null;
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
          yield return PortalAbility.Running;
        } else {
          Mover.GetAxes(AbilityManager, out var desiredMove, out var desiredFacing);
          Mover.UpdateAxes(AbilityManager, desiredMove, toTarget.normalized);
          var aimingTimeout = Fiber.Wait(Timeval.FramesPerSecond*1);
          var aimed = Fiber.Until(() => Vector3.Dot(transform.forward, toTarget.normalized) >= .98f);
          yield return Fiber.Any(aimingTimeout, aimed);
          AbilityManager.TryInvoke(PowerShotAbility.MakeRoutine);
          yield return PowerShotAbility.Running;
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