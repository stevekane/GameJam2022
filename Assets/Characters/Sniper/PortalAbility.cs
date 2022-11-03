using System.Collections;
using UnityEngine;

public class PortalAbility : Ability {
  public LayerMask PortalLayerMask;
  public QueryTriggerInteraction QueryTriggerInteraction;
  public GameObject PortalPrefab;
  public Vector3 ThreatPosition;
  public float RotationSpeed = 360;
  [Range(0,1)]
  public float DirectionalThreshold = .85f;
  public float PortalLaunchHeight = 1;
  [Range(8,32)]
  public int DirectionSamples = 8;
  public Timeval FaceDuration = Timeval.FromSeconds(1);
  public Timeval WaitDuration = Timeval.FromSeconds(1);

  GameObject Portal;

  IEnumerator Face(Transform t, Vector3 direction, float degreesPerSecond, float threshold) {
    while (Vector3.Dot(t.forward, direction) < threshold) {
      var current = t.forward.XZ();;
      var radiansPerSecond = degreesPerSecond*Mathf.Deg2Rad*Time.fixedDeltaTime;
      var next = Vector3.RotateTowards(t.forward, direction, radiansPerSecond, 0);
      t.rotation = Quaternion.LookRotation(next, Vector3.up);
      yield return null;
    }
  }

  float LikelyDistance(
  Vector3 position,
  Vector3 direction,
  float speed,
  float time,
  LayerMask layerMask,
  QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
    var maxDistance = speed*time;
    var didHit = Physics.Raycast(
      position,
      direction,
      out var hit,
      maxDistance,
      layerMask,
      queryTriggerInteraction);
    var finalPosition = didHit ? hit.point : maxDistance*direction;
    // TODO: The approximation of maxdistance being speed*time isn't really good enough.
    // As a result of this, the disk often gets thrown very close to the target which is
    // for sure not the desired behavior
    // Probably need a projectile calculation to make this any good?
    Debug.DrawLine(position, finalPosition, Color.yellow, 1f);
    return Vector3.Distance(position, finalPosition);
  }

  Vector3 SafestDirection(
  Vector3 position,
  Vector3 initialDirection,
  float speed,
  float time,
  LayerMask layerMask,
  QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) {
    var bestDistance = 0f;
    var bestDirection = initialDirection;
    var degreesPerSample = 360/DirectionSamples;
    for (var i = 0; i < DirectionSamples; i++) {
      var rotation = Quaternion.Euler(0, degreesPerSample*i, 0);
      var direction = rotation*initialDirection;
      var distance = LikelyDistance(position, direction, speed, time, layerMask, queryTriggerInteraction);
      Debug.DrawRay(position, direction, Color.red, 1f);
      if (distance > bestDistance) {
        bestDistance = distance;
        bestDirection = direction;
      }
    }
    Debug.DrawRay(position, bestDirection, Color.green, 1f);
    return bestDirection;
  }

  public IEnumerator PortalStart() {
    var trans = AbilityManager.transform;
    var direction = SafestDirection(
      trans.position,
      trans.forward.XZ(),
      PortalPrefab.GetComponent<Projectile>().InitialSpeed,
      WaitDuration.Seconds,
      PortalLayerMask,
      QueryTriggerInteraction);
    var faceTimeout = Fiber.Wait(FaceDuration.Frames);
    var face = Face(trans, direction, RotationSpeed, DirectionalThreshold);
    var faceOutcome = Fiber.Select(faceTimeout, face);
    yield return faceOutcome;
    if (faceOutcome.Value == 0) {
      Debug.LogWarning("Failed to face direction in time");
      Stop();
    } else {
      var portalPosition = PortalLaunchHeight*Vector3.up+trans.position;
      var portalRotation = Quaternion.LookRotation(direction);
      Portal = Instantiate(PortalPrefab, portalPosition, portalRotation);
      yield return Fiber.Wait(WaitDuration.Frames);
      trans.GetComponent<CharacterController>().Move(Portal.transform.position-trans.position);
      Stop();
    }
  }

  public override void Stop() {
    if (Portal) {
      Destroy(Portal);
    }
  }
}