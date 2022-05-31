using UnityEngine;

public class Player : MonoBehaviour {
  public enum PlayerState { Moving, Rolling, Spinning }

  public Controller Controller;
  public PlayerConfig Config;
  public PlayerState State = PlayerState.Moving;
  public Grapple Grapple;
  public PlayerRenderer Renderer;

  Vector3 RollDirection;
  float RollSpeed;
  float RollDuration;
  float RollRemaining;

  void Update() {
    var dt = Time.deltaTime;
    var speed = Config.MoveSpeed;
    var movex = Controller.MoveX;
    var movez = Controller.MoveY;
    var movedirection = new Vector3(movex,0,movez).normalized;
    var moving = movedirection.sqrMagnitude > 0;
    var aimx = Controller.AimX;
    var aimy = Controller.AimY;
    var aimvector = new Vector3(aimx,0,aimy);
    var aimdirection = aimvector.normalized;
    var rotation = transform.rotation;
    var minMagnitude = Config.MinGrappleSquareMagnitude;
    var tryRoll = Controller.Action2;
    var tryHit = Controller.Action1;
    var tryGrapple = aimvector.sqrMagnitude > minMagnitude;
    var grappling = Grapple.State != Grapple.GrappleState.Ready;

    switch (State) {
      case PlayerState.Moving: {
        if (grappling) {
          var towardsHook = (Grapple.Hook.transform.position - transform.position).normalized;
          if (towardsHook.sqrMagnitude > 0) {
            rotation.SetLookRotation(towardsHook,Vector3.up);
            transform.rotation = rotation;
          }
        }

        if (moving) {
          transform.position = dt * speed * movedirection + transform.position;
        }

        if (tryHit) {
          Debug.Log("Hit");
        } else if (tryRoll && moving) {
          RollDirection = movedirection;
          RollSpeed = Config.RollSpeed;
          RollDuration = Config.RollDuration;
          RollRemaining = RollDuration;
          State = PlayerState.Rolling;
        } else if (tryGrapple) {
          if (Grapple.Fire(ref aimdirection)) {
            Debug.Log("Grapple");
          }
        }
      }
      break;

      case PlayerState.Rolling: {
        if (moving) {
          var maxRadians = dt * Config.MaxRollRadiansPerSecond;
          RollDirection = Vector3.RotateTowards(RollDirection,movedirection,maxRadians,0).normalized;
        }

        if (tryGrapple) {
          if (Grapple.Fire(ref aimdirection)) {
            Debug.Log("Grapple While Rolling");
          }
        }

        if (RollRemaining > dt) {
          RollRemaining -= dt;
          transform.position = dt * RollSpeed * RollDirection + transform.position;
        } else if (RollRemaining > 0) {
          transform.position = RollRemaining * RollSpeed * RollDirection + transform.position;
          transform.position = (dt - RollRemaining) * speed * movedirection + transform.position;
          RollRemaining = 0;
          State = PlayerState.Moving;
        } else {
          transform.position = dt * speed * movedirection + transform.position;
          RollRemaining = 0;
          State = PlayerState.Moving;
        }
      }
      break;
    }
  }
}