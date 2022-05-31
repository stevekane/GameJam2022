using UnityEngine;

public class Player : MonoBehaviour {
  public enum PlayerState { Moving }

  public Controller Controller;
  public PlayerConfig Config;
  public PlayerState State = PlayerState.Moving;
  public Grapple Grapple;

  void Update() {
    var dt = Time.deltaTime;
    switch (State) {
      case PlayerState.Moving: {
        var speed = Config.MoveSpeed;
        var movex = Controller.MoveX;
        var movez = Controller.MoveY;
        var movedirection = new Vector3(movex,0,movez).normalized;
        var position = transform.position;
        if (movedirection.sqrMagnitude > 0) {
          transform.position = dt * speed * movedirection + position;
        }
        var aimx = Controller.AimX;
        var aimy = Controller.AimY;
        var aimdirection = new Vector3(aimx,0,aimy).normalized;
        var rotation = transform.rotation;
        if (aimdirection.sqrMagnitude > 0) {
          rotation.SetLookRotation(aimdirection,Vector3.up);
          transform.rotation = rotation;
        }
        var tryGrapple = Controller.Action1;
        if (tryGrapple) {
          if (Grapple.State == Grapple.GrappleState.Ready) {
            Grapple.Hook.transform.SetParent(null,true);
            Grapple.Trajectory = aimdirection;
            Grapple.InFlightRemaining = Config.GrappleFlightDuration;
            Grapple.State = Grapple.GrappleState.InFlight;
          } else {
          }
        }
      }
      break;
    }
  }
}