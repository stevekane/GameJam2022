using UnityEngine;

public class Grapple : MonoBehaviour {
  public enum GrappleState { Ready, InFlight, Attached }

  public GrappleConfig Config;

  public GrappleState State = GrappleState.Ready;
  public Player Player;
  public GrappleHook Hook;
  public Chain Chain;

  public GrappleTarget Target;
  public Vector3 Trajectory;
  public float InFlightRemaining = 2;

  public bool Fire(Vector3 direction) {
    if (State == GrappleState.Ready) {
      Trajectory = direction;
      InFlightRemaining = Config.FlightDuration;
      Hook.transform.SetParent(null,true);
      State = GrappleState.InFlight;
      return true;
    } else {
      return false;
    }
  }

  public bool Attach(GrappleTarget target) {
    if (State == GrappleState.InFlight) {
      Target = target;
      Target.Collider.enabled = false;
      Target.MeshRenderer.material.color = Color.red;
      Hook.transform.SetParent(target.transform,true);
      State = GrappleState.Attached;
      return true;
    } else {
      return false;
    }
  }

  public bool Detach() {
    if (State == GrappleState.Attached) {
      Target.MeshRenderer.material.color = Color.green;
      Target.Collider.enabled = true;
      Target = null;
      Hook.transform.SetParent(transform);
      Hook.transform.SetPositionAndRotation(transform.position,transform.rotation);
      State = GrappleState.Ready;
      return true;
    } else {
      return false;
    }
  }

  void Update() {
    var dt = Time.deltaTime;
    switch (State) {
      case GrappleState.InFlight: {
        InFlightRemaining -= dt;
        if (InFlightRemaining <= 0) {
          Hook.transform.SetParent(transform);
          Hook.transform.SetPositionAndRotation(transform.position,transform.rotation);
          State = GrappleState.Ready;
        } else {
          Hook.transform.position = dt * Config.FlightSpeed * Trajectory + Hook.transform.position;
        }
      }
      break;
    }
  }
}