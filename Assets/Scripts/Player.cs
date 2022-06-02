using UnityEngine;

public class Player : MonoBehaviour {
  public enum PlayerState { Moving, Rolling, Spinning, Zipping }

  public PlayerConfig Config;

  public Controller Controller;
  public CharacterController CharacterController;
  public Grapple Grapple;
  public PlayerState State = PlayerState.Moving;

  Vector3 RollDirection;
  float RollSpeed;
  float RollDuration;
  float RollRemaining;

  Vector3 SpinDirection;
  float SpinSpeed;
  float SpinDuration;
  float SpinRemaining;

  PlayerState BufferedState = PlayerState.Moving;
  int VaultCount = 1;

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
          CharacterController.Move(dt * Physics.gravity);
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
            BufferedState = PlayerState.Moving;
            State = PlayerState.Zipping;
          } else {
            Grapple.Detach();
          }
        } else if (Controller.Grapple.HasValue) {
          Grapple.Fire(Controller.Grapple.Value);
        } else if (tryHit) {
          SpinDirection = movedirection;
          SpinSpeed = Config.SpinSpeed;
          SpinDuration = Config.SpinDuration;
          SpinRemaining = SpinDuration;
          State = PlayerState.Spinning;
        } else if (tryRoll && moving) {
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
          CharacterController.Move(dt * Physics.gravity);
        } else if (RollRemaining > 0) {
          CharacterController.Move(RollRemaining * RollSpeed * RollDirection);
          CharacterController.Move((dt - RollRemaining) * speed * movedirection);
          CharacterController.Move(dt * Physics.gravity);
          RollRemaining = 0;
          State = PlayerState.Moving;
        } else {
          CharacterController.Move(dt * speed * movedirection);
          CharacterController.Move(dt * Physics.gravity);
          RollRemaining = 0;
          State = PlayerState.Moving;
        }
      }
      break;

      case PlayerState.Spinning: {
        if (moving) {
          var maxRadians = dt * Config.MaxSpinRadiansPerSecond;
          SpinDirection = Vector3.RotateTowards(SpinDirection,movedirection,maxRadians,0).normalized;
        }

        var colliders = Physics.OverlapSphere(transform.position, 1);
        for (int i = 0; i < colliders.Length; i++) {
          if (colliders[i].TryGetComponent(out Knockable knockable))
            knockable.Knock((colliders[i].transform.position - transform.position).normalized * 5);
        }

        if (SpinRemaining > dt) {
          SpinRemaining -= dt;
          CharacterController.Move(dt * SpinSpeed * SpinDirection);
          CharacterController.Move(dt * Physics.gravity);
        } else if (SpinRemaining > 0) {
          CharacterController.Move(SpinRemaining * SpinSpeed * SpinDirection);
          CharacterController.Move((dt - SpinRemaining) * speed * movedirection);
          CharacterController.Move(dt * Physics.gravity);
          SpinRemaining = 0;
          State = PlayerState.Moving;
        } else {
          CharacterController.Move(dt * speed * movedirection);
          CharacterController.Move(dt * Physics.gravity);
          SpinRemaining = 0;
          State = PlayerState.Moving;
        }
      }
      break;

      case PlayerState.Zipping: {
        if (Grapple.Target) {
          var destination = Grapple.Hook.transform.position;
          var current = transform.position;
          destination.y = 0;
          var delta = destination - current;
          var distance = delta.magnitude;
          var direction = delta.normalized;
          if (direction.sqrMagnitude > 0) {
            if (distance < Grapple.Config.MinZippingActionRadius)  {
              Grapple.Detach();
              State = BufferedState;
            } else {
              if (tryRoll && moving) {
                RollDirection = movedirection;
                RollSpeed = Config.RollSpeed * VaultCount * Config.RollVaultScaleFactor;
                RollDuration = Config.RollDuration * VaultCount * Config.RollVaultScaleFactor;
                RollRemaining = RollDuration;
                BufferedState = PlayerState.Rolling;
              } else if (tryHit) {
                SpinDirection = movedirection;
                SpinSpeed = Config.SpinSpeed;
                SpinDuration = Config.SpinDuration;
                SpinRemaining = SpinDuration;
                BufferedState = PlayerState.Spinning;
              }
              CharacterController.Move(dt * Grapple.Config.ZipSpeed * direction);
            }
          }
        } else {
          Grapple.Detach();
          State = PlayerState.Moving;
        }
      }
      break;
    }
  }

  public void OnRoomEntered(Room room, Door startingDoor) {
    CharacterController.enabled = false;  // Quick hack until we have a real transition state.
    transform.position = startingDoor.transform.position - startingDoor.transform.position.normalized*2;
    CharacterController.enabled = true;
  }
}