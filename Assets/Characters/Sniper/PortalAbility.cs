using System.Collections;
using UnityEngine;

public class PortalAbility : Ability {
  public IStoppableValue<Vector3> GetPortalDirection;
  public GameObject PortalPrefab;
  public Timeval FaceDuration = Timeval.FromSeconds(1);
  public Timeval WaitDuration = Timeval.FromSeconds(1);

  GameObject Portal;

  public IEnumerator PortalStart() {
    var trans = AbilityManager.transform;
    yield return GetPortalDirection;
    var direction = GetPortalDirection.Value;
    Mover.GetAxes(AbilityManager, out var desiredMove, out var desiredFacing);
    Mover.UpdateAxes(AbilityManager, desiredMove, direction.normalized);
    var aimingTimeout = Fiber.Wait(Timeval.FramesPerSecond*1);
    var aimed = Fiber.Until(() => Vector3.Dot(transform.forward, direction.normalized) >= .98f);
    yield return Fiber.Any(aimingTimeout, aimed);
    var portalPosition = trans.position;
    var portalRotation = Quaternion.LookRotation(direction);
    Portal = Instantiate(PortalPrefab, portalPosition, portalRotation);
    yield return Fiber.Wait(WaitDuration.Frames);
    var deltaXZ = (Portal.transform.position-trans.position).XZ();
    trans.GetComponent<CharacterController>().Move(deltaXZ);
    Stop();
  }

  public override void Stop() {
    if (Portal) {
      Destroy(Portal);
    }
  }
}