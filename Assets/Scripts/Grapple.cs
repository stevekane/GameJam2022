using UnityEngine;

public class Grapple : MonoBehaviour {
  public enum GrappleState { Ready, InFlight, Attached, ToYou, ToThem }

  public GrappleState State = GrappleState.Ready;
  public Player Player;
  public GrappleHook Hook;
  public Chain Chain;

  public Vector3 Trajectory;
  public float InFlightRemaining = 2;

  void Update() {
    var dt = Time.deltaTime;
    switch (State) {
      case GrappleState.Ready: {
      }
      break;

      case GrappleState.InFlight: {
        InFlightRemaining -= dt;
        if (InFlightRemaining <= 0) {
          InFlightRemaining = 0;
          Hook.transform.SetParent(transform,true);
          Hook.transform.SetPositionAndRotation(transform.position,transform.rotation);
          State = GrappleState.Ready;
        } else {
          var position = Hook.transform.position;
          var speed = Player.Config.GrappleSpeed;
          Hook.transform.position = dt * speed * Trajectory + position;
        }
      }
      break;
    }
  }
}