using UnityEngine;

public class Grapple : MonoBehaviour {
  public enum GrappleState { Ready, InFlight, Attached, ToYou, ToThem }

  [SerializeField]
  GrappleConfig Config;

  public GrappleState State = GrappleState.Ready;
  public Player Player;
  public GrappleHook Hook;
  public Chain Chain;

  public Vector3 Trajectory;
  public float InFlightRemaining = 2;

  public bool Fire(Vector3 direction) {
    if (State == GrappleState.Ready) {
      Hook.transform.SetParent(null,true);
      Trajectory = direction;
      InFlightRemaining = Config.FlightDuration;
      State = Grapple.GrappleState.InFlight;
      return true;
    } else {
      return false;
    }
  }

  public bool Attach(GrappleTarget target) {
    if (State == GrappleState.InFlight) {
      Hook.transform.SetParent(target.transform,true);
      State = GrappleState.Attached;
      return true;
    } else {
      return false;
    }
  }

  public void ResetHook() {
    InFlightRemaining = 0;
    Hook.transform.SetParent(transform,true);
    Hook.transform.SetPositionAndRotation(transform.position,transform.rotation);
    State = GrappleState.Ready;
  }

  void ExtendHook(float dt,float speed) {
    Hook.transform.position = dt * speed * Trajectory + Hook.transform.position;
  }

  void Update() {
    var dt = Time.deltaTime;
    switch (State) {
      case GrappleState.Ready: {
      }
      break;

      case GrappleState.InFlight: {
        InFlightRemaining -= dt;
        if (InFlightRemaining <= 0) {
          ResetHook();
        } else {
          ExtendHook(dt,Config.FlightSpeed);
        }
      }
      break;
    }
  }
}