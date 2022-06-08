using System.Collections.Generic;
using UnityEngine;

public enum FlightStatus { None, Windup, Flying, Recovery }
public struct BlockEvent { public Vector3 Position; }
public struct BumpEvent { public Vector3 Position; public Vector3 Velocity; }

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

  List<Targetable> Contacts = new List<Targetable>(32);
  List<BlockEvent> Blocks = new List<BlockEvent>(32);
  List<BumpEvent> Bumps = new List<BumpEvent>(32);

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target-origin;
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DistanceScore.Evaluate(1-distance/Config.SearchRadius);
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

  public void Block(Vector3 position) {
    Blocks.Add(new BlockEvent { Position = position });
  }

  public void Bump(Vector3 position, Vector3 velocity) {
    Bumps.Add(new BumpEvent { Position = position, Velocity = velocity });
  }

  public void Contact(Targetable targetable) {
    Contacts.Add(targetable);
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var move = new Vector3(action.Move.x,0,action.Move.y);
    var aim = new Vector3(action.Aim.x,0,action.Aim.y);
    var grounded = Controller.isGrounded;
    var ui = FindObjectOfType<UI>();
    var targetables = FindObjectsOfType<Targetable>(false);
    var moving = move.magnitude > 0;
    var aiming = aim.magnitude > 0;
    var flying = FlightStatus == FlightStatus.Flying;

    if (grounded && !Perch && aiming && action.PounceDown) {
      Velocity.y = JUMP_VERTICAL_SPEED;
    } else if (grounded) {
      Velocity.y = dt*FALL_GRAVITY;
    } else if (Perch) {
      Velocity.y = 0;
    } else {
      var gravity = Velocity.y < 0 ? FALL_GRAVITY : ASCEND_GRAVITY;
      Velocity.y = Velocity.y+dt*gravity;
    }

    if (grounded && !Perch) {
      Velocity.x = move.x*MOVE_SPEED;
      Velocity.z = move.z*MOVE_SPEED;
    } else if (Perch) {
      Velocity.x = 0;
      Velocity.z = 0;
    }

    if (flying) {
      if (Target && Contacts.Contains(Target)) {
        Target?.PounceTo(this);
        FlightStatus = FlightStatus.None;
        Perch = Target;
      } else if (grounded) {
        FlightStatus = FlightStatus.None;
      }
    }

    if (!flying && aiming && AimingFramesRemaining > 0) {
      Target = FindClosest<Targetable>(Perch,targetables,aim.normalized,transform.position);
      Time.timeScale = Mathf.Lerp(1,.1f,aim.normalized.magnitude);
      AimingFramesRemaining = Mathf.Max(AimingFramesRemaining-1,0);
      ui.Highlight(targetables,targetables.Length);
      ui.Select(Target);
      if (Target && action.PounceDown) {
        var vector = Target.transform.position - transform.position;
        Velocity.x = vector.x*2;
        Velocity.y = 15;
        Velocity.z = vector.z*2;
        Perch?.PounceFrom(this);
        Perch = null;
        FlightStatus = FlightStatus.Flying;
      }
    } else {
      if (!aiming) {
        AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,MAX_AIMING_FRAMES);
      }
      Time.timeScale = 1;
      ui.Highlight(targetables,0);
      ui.Select(null);
    }

    if (!flying) {
      if (aiming) {
        transform.rotation = Quaternion.LookRotation(aim.normalized);
      } else if (move.magnitude > 0) {
        transform.rotation = Quaternion.LookRotation(move.normalized);
      }
    }

    if (Perch) {
      var current = transform.position;
      var target = Perch.transform.position;
      var t = Mathf.Exp(-.1f);
      var next = Vector3.Lerp(target,current,t);
      Controller.Move(next-current);
    } else {
      Controller.Move(dt*Velocity);
    }
    
    var displayAimMeter = !flying && AimingFramesRemaining < MAX_AIMING_FRAMES;
    var aimMeterPosition = transform.position+Vector3.up;
    ui.SetAimMeter(displayAimMeter,aimMeterPosition,AimingFramesRemaining,MAX_AIMING_FRAMES);

    Contacts.Clear();
    Blocks.Clear();
    Bumps.Clear();
  }
}