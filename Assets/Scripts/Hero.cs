using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public struct BlockEvent { public Vector3 Position; }
public struct BumpEvent { public Vector3 Position; public Vector3 Velocity; }

public class Hero : MonoBehaviour {
  public float GRAVITY = -10f;
  public float JUMP_VERTICAL_SPEED = 15f;
  public float JUMP_ANGLE = 30; // 30 degrees from horizontal
  public float POUNCE_SPEED = 2f;
  public float GRABBING_DISTANCE = 3f;
  public float TARGETING_DISTANCE = 1000f;
  public float TARGETING_RADIANS = Mathf.PI/2;
  public float THROW_SPEED = 50f;
  public float PERCH_ATTRACTION_EPSILON = -.1f; // used in framerate-independent exponential lerp
  public int MAX_AIMING_FRAMES = 300;

  public ApeConfig Config;
  public CharacterController Controller;
  public UI UI;
  public Vector3 Velocity;
  public Targetable Perch;
  public Targetable Target;
  public Targetable[] Targets;
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

  bool Within(Vector3 origin, Vector3 target, Vector3 forward, float radians) {
    var delta = target-origin;
    var direction = delta.normalized;
    var dot = Vector3.Dot(direction,forward);
    return dot >= Mathf.Cos(radians);
  }

  Targetable Best(Targetable ignore, Targetable[] targets) {
    Targetable best = null;
    var bestScore = 0f;
    for (int i = 0; i < targets.Length; i++) {
      var targetable = targets[i];
      var score = Score(transform.forward,transform.position,targetable.transform.position);
      var delta = targetable.transform.position-transform.position;
      var distance = delta.magnitude;
      if (targetable != ignore && score > bestScore) {
        best = targetable;
        bestScore = score;
      }
    }
    return best;
  }

  Targetable[] FindTargets(float maxDistance,float maxRadians) {
    var origin = transform.position;
    var forward = transform.forward;
    var includeInactive = false;
    return FindObjectsOfType<Targetable>(includeInactive)
      .Where(t => Within(origin,t.transform.position,forward,maxRadians))
      .ToArray();
  }

  bool Perching { get => Perch; }
  bool Falling { get => !Controller.isGrounded; }
  bool Grounded { get => Controller.isGrounded; }
  bool Moving { get => Inputs.Action.Move.magnitude > 0; }
  bool Aiming { get => Inputs.Action.Aim.magnitude > 0 && AimingFramesRemaining > 0; }

  public void Block(Vector3 position) {
    Blocks.Add(new BlockEvent { Position = position });
  }

  public void Bump(Vector3 position, Vector3 velocity) {
    Bumps.Add(new BumpEvent { Position = position, Velocity = velocity });
  }

  public void Contact(Targetable targetable) {
    Contacts.Add(targetable);
  }

  Quaternion TryLookWith(Vector2 v2, Quaternion q0) {
    return v2.magnitude > 0 ? Quaternion.LookRotation(new Vector3(v2.x,0,v2.y)) : q0;
  }

  Vector3 PullTowards(Vector3 current, Vector3 target, float epsilon, float dt) {
    var next = Vector3.Lerp(target,current,Mathf.Exp(epsilon));
    var delta = next-current;
    return delta/dt;
  }

  // horizontal projectile motion v = (dg/sin(2α))^(1/2)
  // only works when start and end height are the same
  Vector3 JumpVelocity(Vector3 xzDirection, float distance, float angle) {
    var aimRotation = Quaternion.LookRotation(xzDirection,Vector3.up);
    var pitchAxis = aimRotation*Vector3.right;
    var pitchRotation = Quaternion.AngleAxis(-angle,pitchAxis);
    var launchDirection = pitchRotation*xzDirection;
    var magnitude = Mathf.Sqrt(GRAVITY*distance/Mathf.Sin(2*Mathf.Deg2Rad*-angle));
    var velocity = magnitude*launchDirection;
    return velocity;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;

    // Pounce ACTION
    if (Aiming && Target && action.PounceDown) {
      var delta = Target.transform.position-transform.position;
      var direction = delta.normalized;
      var distance = delta.magnitude;
      Velocity = JumpVelocity(direction,distance,JUMP_ANGLE);
      Perch?.PounceFrom(this);
      Perch = null;
      Targets = new Targetable[0];
      transform.rotation = Quaternion.LookRotation(new Vector3(action.Aim.x,0,action.Aim.y));
    // Throw ACTION
    } else if (Perching && Aiming && Target && action.HitDown) {
      var delta = Target.transform.position-transform.position;
      var direction = delta.normalized;
      // TODO: Not really in love w/ this implementation...
      if (Perch.TryGetComponent(out Throwable throwable)) {
        throwable.Throw(THROW_SPEED*direction);
      }
      Perch = null;
      Target = null;
      Targets = new Targetable[0];
      transform.rotation = Quaternion.LookRotation(new Vector3(action.Aim.x,0,action.Aim.y));
    } else if (Perching && Aiming) {
      Velocity = PullTowards(transform.position,Perch.transform.position,PERCH_ATTRACTION_EPSILON,dt);
      Targets = FindTargets(TARGETING_DISTANCE,TARGETING_RADIANS);
      Target = Best(null,Targets);
      transform.rotation = Quaternion.LookRotation(new Vector3(action.Aim.x,0,action.Aim.y));
    // Dismount ACTION
    } else if (Perching && action.PounceDown) {
      var aimRotation = TryLookWith(action.Move,transform.rotation);
      var direction = aimRotation*Vector3.forward;
      Velocity = JumpVelocity(direction,Perch.Radius,JUMP_ANGLE);
      Perch = null;
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = aimRotation;
    } else if (Perching) {
      Velocity = PullTowards(transform.position,Perch.transform.position,PERCH_ATTRACTION_EPSILON,dt);
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = TryLookWith(action.Move,transform.rotation);
    // Jump ACTION
    } else if (Grounded && action.PounceDown) {
      Velocity = new Vector3(action.Move.x*MOVE_SPEED,JUMP_VERTICAL_SPEED,action.Move.y*MOVE_SPEED);
      Perch = null;
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = TryLookWith(action.Move,transform.rotation);
    } else if (Grounded && Aiming) {
      Velocity = new Vector3(action.Move.x*MOVE_SPEED,dt*GRAVITY,action.Move.y*MOVE_SPEED);
      Perch = null;
      Targets = FindTargets(TARGETING_DISTANCE,TARGETING_RADIANS);
      Target = Best(null,Targets);
      transform.rotation = Quaternion.LookRotation(new Vector3(action.Aim.x,0,action.Aim.y));
    } else if (Grounded) {
      Velocity = new Vector3(action.Move.x*MOVE_SPEED,dt*GRAVITY,action.Move.y*MOVE_SPEED);
      Perch = null;
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = TryLookWith(action.Move,transform.rotation);
    // Perch ACTION
    } else if (Falling && Contacts.Count > 0) {
      Velocity = Vector3.zero;
      Perch = Contacts.Contains(Target) ? Target : Contacts[0];
      Perch.PounceTo(this);
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = transform.rotation;
    } else if (Falling && Aiming) {
      Velocity = Velocity+dt*GRAVITY*Vector3.up;
      Perch = null;
      Targets = FindTargets(TARGETING_DISTANCE,TARGETING_RADIANS);
      Target = Best(null,Targets);
      transform.rotation = Quaternion.LookRotation(new Vector3(action.Aim.x,0,action.Aim.y));
    } else if (Falling) {
      Velocity = Velocity+dt*GRAVITY*Vector3.up;
      Perch = null;
      Targets = new Targetable[0];
      Target = null;
      transform.rotation = TryLookWith(action.Move,transform.rotation);
    }
    
    if (action.Aim.magnitude > 0) {
      AimingFramesRemaining = Mathf.Max(AimingFramesRemaining-1,0);
    } else {
      AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,MAX_AIMING_FRAMES);
    }
    Controller.Move(Velocity*dt);
    UI.Select(Target);
    UI.Highlight(Targets,Targets.Length);
    UI.SetAimMeter(transform,AimingFramesRemaining < MAX_AIMING_FRAMES,AimingFramesRemaining,MAX_AIMING_FRAMES);
    Time.timeScale = Aiming ? .1f : 1;
    Contacts.Clear();
    Blocks.Clear();
    Bumps.Clear();
  }
}