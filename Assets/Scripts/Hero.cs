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

  bool Grabbable(Targetable targetable) {
    var delta = targetable.transform.position - transform.position;
    var distance = delta.magnitude;
    return distance < Config.GrabRadius;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);
    var grounded = Controller.isGrounded;
    var ui = FindObjectOfType<UI>();
    var targetables = FindObjectsOfType<Targetable>(false);

    if (grounded && !Perch && aim.magnitude == 0 && action.PounceDown) {
      Velocity.y = JUMP_VERTICAL_SPEED;
    } else if (!Perch) {
      var gravity = Velocity.y < 0 ? FALL_GRAVITY : ASCEND_GRAVITY;
      Velocity.y = Velocity.y + dt * gravity;
    } else if (grounded) {
      Velocity.y = dt * FALL_GRAVITY;
    } else {
      Velocity.y = 0;
    }

    if (grounded && !Perch && move.magnitude > 0) {
      // TODO: This seems like bullshit...
      Velocity.x = move.x * MOVE_SPEED;
      Velocity.z = move.z * MOVE_SPEED;
      transform.rotation.SetLookRotation(move.normalized);
    } else if ((grounded && move.magnitude == 0) || Perch) {
      Velocity.x = 0;
      Velocity.z = 0;
    }

    if (FlightStatus == FlightStatus.Flying) {
      if (Target && Grabbable(Target)) {
        FlightStatus = FlightStatus.None;
        Perch = Target;
      } else if (grounded) {
        FlightStatus = FlightStatus.None;
      }
    }

    if (FlightStatus != FlightStatus.Flying && aim.magnitude > 0 && AimingFramesRemaining > 0) {
      Target = FindClosest<Targetable>(null,targetables,aim.normalized,transform.position);
      Time.timeScale = Mathf.Lerp(1,.1f,aim.normalized.magnitude);
      AimingFramesRemaining = Mathf.Max(AimingFramesRemaining-1,0);
      ui.Highlight(targetables,targetables.Length);
      ui.Select(Target);
      if (Target && action.PounceDown) {
        var vector = Target.transform.position - transform.position;
        var distance = vector.magnitude;
        var direction = vector.normalized;
        Velocity.x = vector.x * 2;
        Velocity.y = 15;
        Velocity.z = vector.z * 2;
        Perch = null;
        FlightStatus = FlightStatus.Flying;
      }
    } else {
      if (aim.magnitude == 0) {
        AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,MAX_AIMING_FRAMES);
      }
      Time.timeScale = 1;
      ui.Highlight(targetables,0);
      ui.Select(null);
    }

    // Hug the target when perched otherwise move
    if (Perch) {
      var current = transform.position;
      var target = Perch.transform.position;
      var t = Mathf.Exp(-.1f);
      var next = Vector3.Lerp(target,current,t);
      Controller.Move(next - current);
    } else {
      Controller.Move(dt * Velocity);
    }
  }
}