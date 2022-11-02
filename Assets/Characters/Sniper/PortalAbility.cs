using System.Collections;
using UnityEngine;

public class PortalAbility : Ability {
  public GameObject PortalPrefab;
  public Vector3 ThreatPosition;
  public float RotationSpeed;
  public float PortalLaunchHeight;
  public Timeval WaitDuration;

  GameObject Portal;

  public IEnumerator PortalStart() {
    var trans = AbilityManager.transform;
    var fromTarget = trans.position-ThreatPosition;
    var direction = fromTarget.normalized;
    var sign = Mathf.Sign(Vector3.Dot(trans.forward, direction));
    while (Vector3.Dot(trans.forward, direction) < .75) {
      trans.RotateAround(trans.position, Vector3.up, sign*RotationSpeed*Time.fixedDeltaTime);
      yield return null;
    }
    var portalPosition = PortalLaunchHeight*Vector3.up+trans.position;
    var portalRotation = Quaternion.LookRotation(direction);
    Portal = Instantiate(PortalPrefab, portalPosition, portalRotation);
    yield return Fiber.Wait(WaitDuration.Frames);
    trans.GetComponent<CharacterController>().Move(Portal.transform.position-trans.position);
    Stop();
  }

  public override void Stop() {
    if (Portal) {
      Destroy(Portal);
    }
  }
}