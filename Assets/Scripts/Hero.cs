using UnityEngine;

public enum FlightStatus { None, Windup, Flying, Recovery }

public class Hero : MonoBehaviour {
  public float ASCEND_GRAVITY = -10f;
  public float FALL_GRAVITY = -30f;
  public float JUMP_VERTICAL_SPEED = 15f;
  public float MOVE_SPEED = 5f;
  public int MAX_AIMING_FRAMES = 300;

  public ApeConfig Config;
  public CharacterController Controller;
  public Vector3 Velocity;
  public FlightStatus FlightStatus;
  public Targetable Target;
  public Targetable Perch;
  public int AimingFramesRemaining;

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target - origin;
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DistanceScore.Evaluate(1 - distance / Config.SearchRadius);
    var b = Config.AngleScore.Evaluate(Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,dot)));
    return a + b;
  }

  T FindClosest<T>(T ignore, MonoBehaviour[] components, Vector3 forward, Vector3 origin) where T : MonoBehaviour {
    T best = null;
    var bestScore = 0f;
    for (int i = 0; i < components.Length; i++) {
      var targetable = components[i].GetComponent<T>();
      var score = Score(forward,origin,components[i].transform.position);
      if (targetable && targetable != ignore && score > bestScore) {
        best = targetable;
        bestScore = score;
      }
    }
    return best;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);
    var grounded = Controller.isGrounded;
    var ui = FindObjectOfType<UI>();
    var targetables = FindObjectsOfType<Targetable>(false);

    // jump if grounded and not perched and not aiming and pounce just down
    if (grounded && !Perch && aim.magnitude == 0 && action.PounceDown) {
      Velocity.y = JUMP_VERTICAL_SPEED;
    // fall when not flying and not perched
    } else if (FlightStatus != FlightStatus.Flying && !Perch) {
      var gravity = Velocity.y < 0 ? FALL_GRAVITY : ASCEND_GRAVITY;
      Velocity.y = Velocity.y + dt * gravity;
    // experience gravity if grounded
    } else if (grounded) {
      Velocity.y = dt * FALL_GRAVITY;
    // do not experience gravity
    } else {
      Velocity.y = 0;
    }

    // run when grounded and not perched and moving
    if (grounded && !Perch && move.magnitude > 0) {
      // TODO: This seems like bullshit...
      Velocity.x = move.x * MOVE_SPEED;
      Velocity.z = move.z * MOVE_SPEED;
      transform.rotation.SetLookRotation(move.normalized);
    // stop if on ground and not moving or perched
    } else if ((grounded && move.magnitude == 0) || Perch) {
      Velocity.x = 0;
      Velocity.z = 0;
    }

    // target and dilate time only if not flying and aiming and aiming frames remain
    if (FlightStatus != FlightStatus.Flying && aim.magnitude > 0 && AimingFramesRemaining > 0) {
      Target = FindClosest<Targetable>(null,targetables,aim.normalized,transform.position);
      Time.timeScale = Mathf.Lerp(1,.1f,aim.normalized.magnitude);
      AimingFramesRemaining = Mathf.Max(AimingFramesRemaining-1,0);
      ui.Highlight(targetables,targetables.Length);
      ui.Select(Target);
      // pounce if target and pounce just down
      if (Target && action.PounceDown) {
        Debug.Log("Pounce init");
      }
    } else {
      // recharge if not aiming 
      if (aim.magnitude == 0) {
        AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,MAX_AIMING_FRAMES);
      }
      Target = null;
      Time.timeScale = 1;
      ui.Highlight(targetables,0);
      ui.Select(null);
    }

    // move by velocity
    Controller.Move(dt * Velocity);
  }
}