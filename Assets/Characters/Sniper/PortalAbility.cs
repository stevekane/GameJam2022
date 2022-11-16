using System.Collections;
using UnityEngine;

public class PortalAbility : Ability {
  public IStoppableValue<Vector3> GetPortalDirection;
  public GameObject PortalPrefab;
  public Timeval FaceDuration = Timeval.FromSeconds(1);
  public Timeval WaitDuration = Timeval.FromSeconds(1);

  GameObject Portal;

  public IEnumerator PortalStart() {
    yield return GetPortalDirection;
    var direction = GetPortalDirection.Value;
    yield return AbilityManager.GetComponent<Mover>().TryAimAt(direction, FaceDuration);
    Portal = Instantiate(PortalPrefab, transform.position, transform.rotation);
    yield return Fiber.Wait(WaitDuration.Ticks);
    var deltaXZ = (Portal.transform.position-AbilityManager.transform.position).XZ();
    AbilityManager.GetComponent<CharacterController>().Move(deltaXZ);
  }

  public override void OnStop() {
    Portal.Destroy();
  }
}