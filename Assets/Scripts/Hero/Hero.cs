using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// TODO: These don't need to be public if they are not used publicly below
public struct BlockEvent { public Vector3 Position; }
public struct BumpEvent { public Vector3 Position; public Vector3 Velocity; }
public enum ArmState { Free, Reaching, Pulling, Holding }

public class Hero : MonoBehaviour {
  [Header("Configuration")]
  [SerializeField] HeroConfig Config;

  [Header("Components")]
  [SerializeField] UI UI;
  [SerializeField] CharacterController Controller;
  
  [Header("Targeting State")]
  public Targetable Target;
  public Targetable[] Targets;
  public int AimingFramesRemaining;

  [Header("Arm State")]
  public ArmState ArmState;
  public Throwable ArmTarget;
  public float ArmFramesRemaining;

  [Header("Leg State")]
  public Vector3 Velocity;
  public Targetable LegTarget;
  public float AirTime;

  List<GameObject> Entered = new List<GameObject>(32);
  List<GameObject> Stayed = new List<GameObject>(32);
  List<GameObject> Exited = new List<GameObject>(32);
  List<BlockEvent> Blocks = new List<BlockEvent>(32);
  List<BumpEvent> Bumps = new List<BumpEvent>(32);

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target-origin;
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DISTANCE_SCORE.Evaluate(1-distance/Config.MAX_TARGETING_DISTANCE);
    var b = Config.ANGLE_SCORE.Evaluate(Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,dot)));
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

  // TODO: This is dog-slow due to allocation. Probably some way to replace
  // this with a simple cached global system for Targetables or some similarly
  // annoying solution.
  Targetable[] FindTargets(float maxDistance,float maxRadians) {
    var origin = transform.position;
    var forward = transform.forward;
    var includeInactive = false;
    return FindObjectsOfType<Targetable>(includeInactive)
      .Where(t => Vector3.Distance(t.transform.position,transform.position) <= maxDistance)
      .Where(t => Within(origin,t.transform.position,forward,maxRadians))
      .ToArray();
  }

  T GetFirst<T>(List<GameObject> gameObjects) where T : MonoBehaviour {
    foreach (var go in gameObjects) {
      if (go.TryGetComponent(out T t)) {
        return t;
      }
    }
    return null;
  }

  bool TryGetFirst<T>(List<GameObject> gameObjects, out T t) where T : MonoBehaviour {
    t = GetFirst<T>(gameObjects);
    return t;
  }

  bool Free { get => ArmState == ArmState.Free; }
  bool Reaching { get => ArmState == ArmState.Reaching; }
  bool Pulling { get => ArmState == ArmState.Pulling; }
  bool Holding { get => ArmState == ArmState.Holding; }
  bool Perching { get => LegTarget; }
  bool Falling { get => !Controller.isGrounded && !LegTarget; }
  bool Grounded { get => Controller.isGrounded && !LegTarget; }
  bool Moving { get => Inputs.Action.Move.magnitude > 0; }
  bool Aiming { get => Inputs.Action.Aim.magnitude > 0 && AimingFramesRemaining > 0; }

  public void Block(Vector3 position) {
    Blocks.Add(new BlockEvent { Position = position });
  }

  public void Bump(Vector3 position, Vector3 velocity) {
    Bumps.Add(new BumpEvent { Position = position, Velocity = velocity });
  }

  public void Enter(GameObject gameObject) {
    Entered.Add(gameObject);
  }

  public void Stay(GameObject gameObject) {
    Stayed.Add(gameObject);
  }

  public void Exit(GameObject gameObject) {
    Exited.Add(gameObject);
  }

  Quaternion TryLookWith(Vector2 v2, Quaternion q0) {
    return v2.magnitude > 0 ? Quaternion.LookRotation(new Vector3(v2.x,0,v2.y)) : q0;
  }

  Vector3 MoveAcceleration(Vector3 desiredMove, float dt) {
    var currentVelocity = new Vector3(Velocity.x,0,Velocity.z);
    var desiredVelocity = desiredMove*Config.MOVE_SPEED;
    var acceleration = desiredVelocity-currentVelocity;
    var direction = acceleration.normalized;
    var magnitude = Mathf.Min(Config.MAX_XZ_ACCELERATION,acceleration.magnitude);
    var gravity = Config.GRAVITY*Vector3.up;
    return magnitude*direction+dt*gravity;
  }

  Vector3 FallAcceleration(Vector3 desiredMove, float dt) { 
    var normalizedTime = AirTime/Config.MAX_STEERING_TIME;
    var power = Config.MAX_STEERING_MULTIPLIER*Config.STEERING_STRENGTH.Evaluate(normalizedTime);
    var gravityFactor = Velocity.y < 0 ? Config.FALL_GRAVITY_MULTIPLIER: 1;
    var gravity = gravityFactor*Config.GRAVITY*Vector3.up;
    var steering = power*desiredMove;
    return dt*(gravity+steering);
  }

  void Perch(Targetable targetable) {
    LegTarget = targetable;
    LegTarget?.PounceTo(this);
    Velocity = Vector3.zero;
  }

  void Jump(Vector3 move) {
    var boost = Config.JUMP_XZ_MULTIPLIER;
    var speed = Config.MOVE_SPEED;
    var upward = Config.JUMP_Y_VELOCITY;
    AirTime = 0;
    LegTarget?.PounceFrom(this);
    LegTarget = null;
    Velocity = new Vector3(move.x*speed*boost,upward,move.z*speed*boost);
  }

  void Pounce(Vector3 destination) {
    var delta = destination-transform.position;
    var distance = delta.magnitude;
    var move = delta.normalized;
    var boost = distance;
    var speed = Config.MOVE_SPEED;
    var upward = Config.JUMP_Y_VELOCITY/2;
    AirTime = 0;
    LegTarget?.PounceFrom(this);
    LegTarget = null;
    Velocity = new Vector3(move.x*speed*boost,upward,move.z*speed*boost);
  }

  void Walk(Vector3 move) {
    LegTarget = null;
    Velocity = Config.MOVE_SPEED*move;
  }

  void Reach(Throwable throwable) {
    ArmTarget = throwable;
    ArmState = ArmState.Reaching;
    ArmFramesRemaining = Config.MAX_REACHING_FRAMES;
  }

  void Pull(Throwable throwable) {
    ArmTarget = throwable;
    ArmState = ArmState.Pulling;
    ArmFramesRemaining = Config.MAX_PULLING_FRAMES;
  }

  void Hold(Throwable throwable) {
    ArmState = ArmState.Holding;
    ArmTarget = throwable;
  }

  void Throw(Vector3 direction, float speed) {
    ArmTarget?.Throw(speed*direction);
    ArmTarget = null;
    ArmState = ArmState.Free;
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var targetingDistance = Config.MAX_TARGETING_DISTANCE;
    var targetingRadians = Config.MAX_TARGETING_ANGLE*Mathf.Deg2Rad;

    Targets = FindTargets(targetingDistance,targetingRadians);
    Target = Best(LegTarget,Targets);

    /*
    Implement Throw
    Implement Power Throw?
    Implement Reach / Pull with placeholder graphics
    */

    // TODO: Should there be two transitions: Arms and Legs instead of a single disjunction
    if (Falling && TryGetFirst(Entered,out Targetable targetable)) {
      Perch(targetable);
    } else if (Aiming && Target && action.PounceDown) {
      Pounce(Target.transform.position);
    } else if (Aiming && Free && Target && action.HitDown && Target.TryGetComponent(out Throwable distantThrowable)) {
      Reach(distantThrowable);
    } else if ((Grounded || Perching) && !Aiming && action.PounceDown) {
      Jump(action.MoveXZ);
    } else if (Grounded && !Aiming && action.HitDown && TryGetFirst(Stayed,out Throwable throwable)) {
      Hold(throwable);
    } else if (Reaching && ArmFramesRemaining <= 0) {
      Pull(ArmTarget);
    } else if (Pulling && ArmFramesRemaining <= 0) {
      Hold(ArmTarget);
    } else if (Grounded) {
      Walk(action.MoveXZ);
    }

    if (LegTarget) {
      var target = LegTarget.transform.position;
      var current = transform.position;
      var interpolant = Mathf.Exp(Config.PERCH_ATTRACTION_EPSILON);
      var next = Vector3.Lerp(target,current,interpolant);
      Velocity = next-current;
      Controller.Move(Velocity);
    } else if (Grounded) {
      Velocity += MoveAcceleration(action.MoveXZ,dt);
      Controller.Move(dt*Velocity);
    } else {
      AirTime += dt;
      Velocity += FallAcceleration(action.MoveXZ,dt);
      Controller.Move(dt*Velocity);
    } 

    if (Aiming) {
      transform.forward = action.AimXZ;
    } else if (Reaching || Pulling) {
      var targetXZ = ArmTarget.transform.position;
      var currentXZ = transform.position;
      targetXZ.y = 0;
      currentXZ.y = 0;
      transform.rotation = Quaternion.LookRotation(targetXZ-currentXZ);
    } else if ((Free || Holding) && Moving) {
      transform.forward = action.MoveXZ;
    }

    if (Holding) {
      var target = transform.position+2*Vector3.up;
      var current = ArmTarget.transform.position;
      var interpolant = Mathf.Exp(Config.HOLD_ATTRACTION_EPSILON);
      var next = Vector3.Lerp(target,current,interpolant);
      ArmTarget.transform.position = next;
    } else if (Reaching) {
      ArmFramesRemaining = Mathf.Max(0,ArmFramesRemaining-1);
    } else if (Pulling) {
      ArmFramesRemaining = Mathf.Max(0,ArmFramesRemaining-1);
    }

    if (action.Aim.magnitude > 0) {
      UI.Select(Target);
      UI.Highlight(Targets,Targets.Length);
      AimingFramesRemaining = Mathf.Max(0,AimingFramesRemaining-1);
      Time.timeScale = .1f;
    } else {
      AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,Config.MAX_TARGETING_FRAMES);
      Time.timeScale = 1;
      UI.Select(null);
      UI.Highlight(Targets,0);
    }

    {
      var maxTargetingFrames = Config.MAX_TARGETING_FRAMES;
      var displayMeter = AimingFramesRemaining < maxTargetingFrames;
      UI.SetAimMeter(transform,displayMeter,AimingFramesRemaining,maxTargetingFrames);
    }

    Entered.Clear();
    Stayed.Clear();
    Exited.Clear();
    Blocks.Clear();
    Bumps.Clear();
  }
}