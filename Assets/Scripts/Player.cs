using UnityEngine;

public class Player : MonoBehaviour {
  public enum PlayerState { Moving, Rolling, Spinning }

  public PlayerConfig Config;

  public Controller Controller;
  public CharacterController CharacterController;
  public Grapple Grapple;
  public PlayerState State = PlayerState.Moving;

  Vector3 RollDirection;
  float RollSpeed;
  float RollDuration;
  float RollRemaining;

  void Update() {
    var dt = Time.deltaTime;
    var movex = Controller.MoveX;
    var movez = Controller.MoveY;
    var movevector = new Vector3(movex,0,movez);
    var movedirection = movevector.normalized;
    var speed = Config.MoveSpeed * Config.MovementCurve.Evaluate(movevector.magnitude);
    var moving = movedirection.sqrMagnitude > 0;
    var rotation = transform.rotation;
    var tryRoll = Controller.Action2;
    var tryHit = Controller.Action1;

    switch (State) {
      case PlayerState.Moving: {
        if (moving) {
          CharacterController.Move(dt * speed * movedirection);
        }

        if (Grapple.State != Grapple.GrappleState.Ready) {
          var towardsHook = (Grapple.Hook.transform.position - transform.position).normalized;
          if (towardsHook.sqrMagnitude > 0) {
            rotation.SetLookRotation(towardsHook,Vector3.up);
            transform.rotation = rotation;
          }
        }

        if (Grapple.State == Grapple.GrappleState.Attached && Controller.Grapple.HasValue) {
          var towardsHook = (Grapple.Hook.transform.position - transform.position).normalized;
          if (Vector3.Dot(towardsHook,Controller.Grapple.Value) > 0) {
            // TODO: Send Charge to the Grapple
          } else {
            // TODO: Send Pull to the Grapple
            Grapple.ResetHook();
          }
        } else if (Controller.Grapple.HasValue) {
          Grapple.Fire(Controller.Grapple.Value);
        } else if (tryHit) {
          Debug.Log("Hit");
        } else if (tryRoll) {
          RollDirection = movedirection;
          RollSpeed = Config.RollSpeed;
          RollDuration = Config.RollDuration;
          RollRemaining = RollDuration;
          State = PlayerState.Rolling;
        }
      }
      break;

      case PlayerState.Rolling: {
        if (moving) {
          var maxRadians = dt * Config.MaxRollRadiansPerSecond;
          RollDirection = Vector3.RotateTowards(RollDirection,movedirection,maxRadians,0).normalized;
        }

        if (Controller.Grapple.HasValue) {
          Grapple.Fire(Controller.Grapple.Value);
        }

        if (RollRemaining > dt) {
          RollRemaining -= dt;
          CharacterController.Move(dt * RollSpeed * RollDirection);
        } else if (RollRemaining > 0) {
          CharacterController.Move(RollRemaining * RollSpeed * RollDirection);
          CharacterController.Move((dt - RollRemaining) * speed * movedirection);
          RollRemaining = 0;
          State = PlayerState.Moving;
        } else {
          CharacterController.Move(dt * speed * movedirection);
          RollRemaining = 0;
          State = PlayerState.Moving;
        }
      }
      break;
    }
  }
}