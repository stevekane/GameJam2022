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

  // TODO: This could be made a value-yielding routine
  IEnumerator AcquireTarget() {
    Target = null;
    while (!Target) {
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
      yield return null;
    }
  }

  IEnumerator LookAround() {
    var randXZ = UnityEngine.Random.insideUnitCircle;
    var direction = new Vector3(randXZ.x, 0, randXZ.y);
    Mover.GetAxes(AbilityManager, out var move, out var forward);
    Mover.UpdateAxes(AbilityManager, move, direction);
    yield return Fiber.Wait(Timeval.FramesPerSecond);
  }

  IEnumerator BaseBehavior() {
    var accquireTarget = AcquireTarget();
    var lookAround = Fiber.Repeat(LookAround);
    yield return Fiber.Any(accquireTarget, lookAround);
    var toTarget = Target.position-transform.position;
    if (toTarget.magnitude < MinDistance) {
      PortalAbility.GetPortalDirection = new ChooseRandomDirection();
      AbilityManager.TryInvoke(PortalAbility.PortalStart);
      yield return PortalAbility.Running;
    } else {
      yield return Mover.TryAimAt(toTarget.normalized, Timeval.FromSeconds(1));
      AbilityManager.TryInvoke(PowerShotAbility.MakeRoutine);
      yield return PowerShotAbility.Running;
    }
  }

  void Start() => Bundle.StartRoutine(new Fiber(Fiber.Repeat(BaseBehavior)));
  void OnDestroy() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}