using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public struct BlockEvent { public Vector3 Position; public Vector3 Velocity; }
public enum ArmState { Free, Reaching, Pulling, Holding, Attacking }

public static class PhysicsLayers {
  public static readonly int PlayerGrounded = 8;
  public static readonly int PlayerAirborne = 9;
  public static readonly int MobGrounded = 10;
  public static readonly int MobAirborne = 11;
}

public class Hero : MonoBehaviour {
  [Header("Configuration")]
  [SerializeField] HeroConfig Config;

  [Header("Components")]
  [SerializeField] UI UI;
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;
  [SerializeField] Attacker Attacker;
  [SerializeField] Grabber Grabber;
  [SerializeField] AudioSource FootstepAudioSource;
  [SerializeField] AudioSource LegAudioSource;
  [SerializeField] AudioSource ArmAudioSource;

  [Header("Targeting State")]
  public LayerMask TargetableMobLayerMask;
  public Targetable Target;
  public Targetable[] Targets;
  public int AimingFramesRemaining;

  [Header("Arm State")]
  public ArmState ArmState;
  public Throwable ArmTarget;
  public int ArmFramesRemaining;

  [Header("Leg State")]
  public Targetable LegTarget;
  public Targetable LastPerch;
  public Vector3 Velocity;
  public Vector3 DashVelocity;
  public int FootstepFramesRemaining;
  public int PounceFramesRemaining;
  public int DashFramesRemaining;
  public float AirTime;
  public int JumpType = 0;
  public Vector3 LastGroundPosition;

  [Header("Status Effects")]
  public Vector3 BumpVelocity;
  public float BumpTimeRemaining;
  public float StunTimeRemaining;

  List<GameObject> Entered = new List<GameObject>(32);
  List<GameObject> Stayed = new List<GameObject>(32);
  List<GameObject> Exited = new List<GameObject>(32);
  List<BlockEvent> Blocks = new List<BlockEvent>(32);

  float Score(Vector3 forward, Vector3 origin, Vector3 target) {
    var delta = target.XZ()-origin.XZ();
    var distance = delta.magnitude;
    var dot = distance > 0 ? Vector3.Dot(delta.normalized,forward) : 1;
    var a = Config.DISTANCE_SCORE.Evaluate(1-distance/Config.MAX_TARGETING_DISTANCE);
    var b = Config.ANGLE_SCORE.Evaluate(Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,dot)));
    return a+b;
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
    var forward = transform.forward;
    var origin = transform.position;
    foreach (var targetable in targets) {
      var target = targetable.transform.position;
      var score = Score(forward,origin,target);
      if (targetable != ignore && score > bestScore) {
        best = targetable;
        bestScore = score;
      }
    }
    return best;
  }

  Targetable[] FindTargets(float maxDistance, float maxRadians) {
    var origin = transform.position;
    var forward = transform.forward;
    return FindObjectsOfType<Targetable>(includeInactive:false)
      .Where(t => {
        var delta = t.transform.position-origin;
        var distance = delta.magnitude;
        var direction = delta.normalized;
        var didHit = Physics.Raycast(origin,direction,out RaycastHit hit,distance,TargetableMobLayerMask);
        var inPlanarRange = Vector3.Distance(origin.XZ(),t.transform.position.XZ()) <= maxDistance;
        var withinView = Within(origin,t.transform.position,forward,maxRadians);
        var hitTarget = hit.collider && hit.collider.gameObject == t.gameObject;
        return hitTarget && inPlanarRange && withinView;
      })
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

  bool Stunned { get => StunTimeRemaining > 0; }
  bool Bumped { get => BumpTimeRemaining > 0; }
  bool Free { get => ArmState == ArmState.Free; }
  bool Reaching { get => ArmState == ArmState.Reaching; }
  bool Pulling { get => ArmState == ArmState.Pulling; }
  bool Holding { get => ArmState == ArmState.Holding; }
  bool Perching { get => LegTarget; }
  bool Falling { get => !Controller.isGrounded && !LegTarget; }
  bool Grounded { get => Controller.isGrounded && !LegTarget; }
  bool Dashing { get => DashFramesRemaining > 0; }
  bool Pouncing { get => !Perching && PounceFramesRemaining > 0; }
  bool Moving { get => Inputs.Action.Move.XZ.magnitude > 0; }
  bool Aiming { get => Inputs.Action.Aim.XZ.magnitude > 0; }

  public void Block(Vector3 position, Vector3 velocity) {
    Blocks.Add(new BlockEvent { Position = position, Velocity = velocity });
  }

  public void Bump(Vector3 position, Vector3 velocity, float bumpDuration) {
    BumpTimeRemaining = bumpDuration;
    BumpVelocity = velocity;
  }

  public void Stun(float duration) {
    StunTimeRemaining = 0;
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

  // TODO: Review this... it seems that acceleration should be dV/dt
  // this implies that the acceleration we are computing should be
  // divided by dt before being compared to max acceleration
  Vector3 MoveAcceleration(Vector3 desiredMove,float speed) {
    var currentVelocity = new Vector3(Velocity.x,0,Velocity.z);
    var desiredVelocity = desiredMove*speed;
    var acceleration = desiredVelocity-currentVelocity;
    var direction = acceleration.normalized;
    var magnitude = Mathf.Min(Config.MAX_XZ_ACCELERATION,acceleration.magnitude);
    return magnitude*direction;
  }

  Vector3 FallAcceleration(Vector3 desiredMove, float dt) { 
    var normalizedTime = AirTime/Config.MAX_STEERING_TIME;
    var multiplier = Config.MAX_STEERING_MULTIPLIER;
    var strength = Config.STEERING_STRENGTH.Evaluate(normalizedTime);
    var forward = Velocity.XZ().normalized;
    var right = new Vector3(forward.z,0,-forward.x);
    var aSteer = Vector3.Project(desiredMove,right);
    var aSlow = Vector3.Project(desiredMove,-forward);
    var dot = Vector3.Dot(forward,aSlow);
    aSlow = dot > 0 ? Vector3.zero : aSlow;
    var steering = multiplier*strength*(aSteer+aSlow);
    var gravityFactor = Velocity.y < 0 ? Config.FALL_GRAVITY_MULTIPLIER: 1;
    var gravity = gravityFactor*Config.GRAVITY*Vector3.up;
    return dt*(gravity+steering);
  }

  void Perch(Targetable targetable) {
    AirTime = 0;
    LegTarget = targetable;
    LastPerch = LegTarget;
    LegTarget?.PounceTo(this);
    Velocity = Vector3.zero;
    PounceFramesRemaining = 0;
    LegAudioSource.PlayOneShot(Config.PerchAudioClip);
  }

  void Jump(Vector3 move) {
    var boost = Config.JUMP_XZ_MULTIPLIER;
    var speed = Config.MOVE_SPEED;
    var upward = Config.JUMP_Y_VELOCITY;
    AirTime = 0;
    Velocity = new Vector3(move.x*speed*boost,upward,move.z*speed*boost);
    JumpType = 0;
    LegAudioSource.PlayOneShot(Config.JumpAudioClip);
  }

  void Leap(Vector3 move) {
    var boost = Config.JUMP_XZ_MULTIPLIER;
    var speed = Config.MOVE_SPEED;
    var upward = Config.JUMP_Y_VELOCITY;
    var direction = move.magnitude > 0 ? move.normalized : transform.forward;
    AirTime = 0;
    LegTarget?.PounceFrom(this);
    LegTarget = null;
    Velocity = new Vector3(direction.x*speed*boost,upward,direction.z*speed*boost);
    JumpType = 1;
    LegAudioSource.PlayOneShot(Config.LeapAudioClip);
  }

  void Pounce(Vector3 destination) {
    var delta = destination-transform.position;
    var duration = (float)Config.MAX_POUNCE_FRAMES*Time.fixedDeltaTime;
    AirTime = 0;
    PounceFramesRemaining = Config.MAX_POUNCE_FRAMES;
    LegTarget?.PounceFrom(this);
    LegTarget = null;
    Velocity = delta/duration;
    LegAudioSource.PlayOneShot(Config.PounceAudioClip);
  }

  void Dash(Vector3 direction) {
    Velocity = direction*Config.DASH_SPEED;
    DashFramesRemaining = Config.MAX_DASH_FRAMES;
    LegAudioSource.PlayOneShot(Config.DashAudioClip);
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
    ArmTarget.Hold();
    ArmAudioSource.PlayOptionalOneShot(Config.HoldAudioClip);
  }

  void Throw(Vector3 direction, float speed) {
    ArmTarget?.Throw(speed*direction);
    ArmTarget = null;
    ArmState = ArmState.Free;
    ArmAudioSource.PlayOptionalOneShot(Config.ThrowAudioClip);
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;
    var targetingDistance = Config.MAX_TARGETING_DISTANCE;
    var targetingRadians = Config.MAX_TARGETING_ANGLE*Mathf.Deg2Rad;

    Targets = FindTargets(targetingDistance,targetingRadians);
    Target = Best(LegTarget,Targets);

    if (Grounded && !Dashing && action.Dash.JustDown) {
      Dash(transform.forward);
    } else if (Free && !Attacker.IsAttacking && !Stunned && !Dashing && action.Light.JustDown) {
      Attacker.StartAttack(0);
    } else if (Free && !Attacker.IsAttacking && !Stunned && !Dashing && action.Heavy.JustDown) {
      Attacker.StartAttack(1);
    } else if (Free && !Attacker.IsAttacking && !Stunned && !Dashing && action.Throw.JustDown) {
      Attacker.StartAttack(2);
    } else if (Falling && !Bumped && TryGetFirst(Entered, out Targetable targetable)) {
      Perch(targetable);
    } else if (Aiming && Target && !Stunned && action.Jump.JustDown) {
      Pounce(Target.transform.position);
    } else if (Grounded && !Aiming && !Stunned && action.Jump.JustDown) {
      Jump(action.Move.XZ);
    } else if (Perching && !Aiming && !Stunned && action.Jump.JustDown) {
      Leap(action.Move.XZ);
    } else if (Holding && !Stunned && action.Hit.JustDown) {
      Throw(transform.forward, Config.THROW_SPEED);
    } else if (Aiming && Free && !Stunned && action.Hit.JustDown && Target && Target.TryGetComponent(out Throwable distantThrowable)) {
      Reach(distantThrowable);
    } else if (Grounded && !Aiming && !Stunned && action.Hit.JustDown && TryGetFirst(Stayed, out Throwable throwable)) {
      Hold(throwable);
    } else if (Reaching && ArmFramesRemaining <= 0) {
      Pull(ArmTarget);
    } else if (Pulling && ArmFramesRemaining <= 0) {
      Hold(ArmTarget);
    }

    Attacker.Step(dt);

    if (Blocks.Count > 0) {
      LegTarget = null;
      Velocity = Blocks[0].Velocity;
    }
    if (BumpTimeRemaining > 0) {
      LegTarget = null;
      BumpTimeRemaining -= dt;
    }
    if (StunTimeRemaining > 0) {
      StunTimeRemaining -= dt;
    }

    if (LegTarget) {
      var target = LegTarget.transform.position+LegTarget.Height*Vector3.up;
      var current = transform.position;
      var interpolant = Mathf.Exp(Config.PERCH_ATTRACTION_EPSILON);
      var next = Vector3.Lerp(target, current, interpolant);
      Velocity = next-current;
      Controller.Move(Velocity);
      Animator.SetInteger("LegState",2);
    } else if (Attacker.IsAttacking) {
      // No movement during attack
      Velocity += MoveAcceleration(action.Move.XZ,Config.MOVE_SPEED*Attacker.MoveFactor);
      Velocity.y = Velocity.y > 0 ? Velocity.y : -1;
      Controller.Move(dt*Velocity);
      Animator.SetInteger("LegState",0);
    } else if (Stunned || Bumped) {
      // This is hacky as fuck but hopefully temporary.
      float vy = Velocity.y;
      Velocity = Bumped ? BumpVelocity : Vector3.zero;
      if (Falling) {
        AirTime += dt;
        Velocity.y = vy + FallAcceleration(Vector3.zero, dt).y;
      }
      Controller.Move(dt*Velocity);
      Animator.SetInteger("LegState",0);
    } else if (Pouncing) {
      PounceFramesRemaining = Mathf.Max(0,PounceFramesRemaining-1);
      if (Target) {
        Velocity = (Target.transform.position - transform.position).normalized * Velocity.magnitude;
      }
      Controller.Move(dt*Velocity);
      Animator.SetInteger("LegState",1);
    } else if (Dashing) {
      DashFramesRemaining = Mathf.Max(0,DashFramesRemaining-1);
      Controller.Move(dt*Velocity);
    } else if (Grounded) {
      if (FootstepFramesRemaining <= 0) {
        FootstepFramesRemaining = Config.FramesPerFootstep;
        if (Moving) {
          FootstepAudioSource.PlayOneShot(Config.RunningAudioClip);
        }
      } else {
        FootstepFramesRemaining = Mathf.Max(0,FootstepFramesRemaining-1);
      }
      Velocity += MoveAcceleration(action.Move.XZ,Config.MOVE_SPEED);
      Velocity.y = Velocity.y > 0 ? Velocity.y : -1;
      Controller.Move(dt*Velocity);
      Animator.SetInteger("LegState",0);
      LastPerch = null;
    } else {
      var wasGrounded = Grounded;
      AirTime += dt;
      Velocity += FallAcceleration(action.Move.XZ,dt);
      Controller.Move(dt*Velocity);
      var isGrounded = Grounded;
      if (!wasGrounded && isGrounded) {
        AirTime = 0;
        LegAudioSource.PlayOneShot(Config.LandAudioClip);
      }
      Animator.SetFloat("VerticalSpeed",Velocity.y);
      Animator.SetInteger("LegState",1);
      Animator.SetFloat("JumpType",(float)JumpType);
    }

    if ((Grounded || Perching) && !Stunned && !Attacker.IsAttacking) {
      if (Aiming) {
        transform.forward = action.Aim.XZ;
      } else if (Reaching || Pulling) {
        var targetXZ = ArmTarget.transform.position;
        var currentXZ = transform.position;
        targetXZ.y = 0;
        currentXZ.y = 0;
        transform.rotation = Quaternion.LookRotation(targetXZ-currentXZ);
      } else if ((Free || Holding) && Moving) {
        transform.forward = action.Move.XZ;
      }
    } else {
      var forward = Velocity.XZ().normalized;
      if (forward.magnitude > 0) {
        transform.forward = forward;
      }
    }

    if (Attacker.IsAttacking) {
      // Handled by Attacker
    } else if (Holding) {
      var target = transform.position+2*Vector3.up;
      var current = ArmTarget.transform.position;
      var interpolant = Mathf.Exp(Config.HOLD_ATTRACTION_EPSILON);
      var next = Vector3.Lerp(target,current,interpolant);
      Grabber.Store(Grabber.transform.position);
      ArmFramesRemaining = 0;
      ArmTarget.transform.position = next;
    } else if (Reaching) {
      Grabber.Reach(Grabber.transform,ArmTarget.transform,ArmFramesRemaining,Config.MAX_REACHING_FRAMES);
      ArmFramesRemaining = Mathf.Max(0,ArmFramesRemaining-1);
    } else if (Pulling) {
      Grabber.Store(Grabber.transform.position);
      ArmFramesRemaining = Mathf.Max(0,ArmFramesRemaining-1);
    } else {
      Grabber.Store(Grabber.transform.position);
      ArmFramesRemaining = 0;
    }

    {
      var forward = transform.forward;
      var right = transform.right;
      var velocityxz = new Vector3(Velocity.x,0,Velocity.z);
      var f = Vector3.Dot(velocityxz,forward);
      var r = Vector3.Dot(velocityxz,right);
      var a = new Vector3(r,0,f);
      Animator.SetFloat("Forward",a.z);
      Animator.SetFloat("Right",a.x);
    }

    // Reset after falling.
    if (Grounded) {
      LastGroundPosition = transform.position;
    } else if (AirTime > 2f) {
      Controller.enabled = false;
      transform.position = LastGroundPosition;
      Controller.enabled = true;
      Velocity = Vector3.zero;
    }

    if (!Pouncing && !Stunned && Free && action.Aim.XZ.magnitude > 0) {
      UI.Select(Target);
      UI.Highlight(Targets,Targets.Length);
      AimingFramesRemaining = Mathf.Max(0,AimingFramesRemaining-1);
      Animator.SetFloat("Aim",1);
    } else {
      UI.Select(null);
      UI.Highlight(Targets,0);
      AimingFramesRemaining = Mathf.Min(AimingFramesRemaining+1,Config.MAX_TARGETING_FRAMES);
      Animator.SetFloat("Aim",0);
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

    if (Grounded && !Pouncing) {
      gameObject.layer = PhysicsLayers.PlayerGrounded;
    } else {
      gameObject.layer = PhysicsLayers.PlayerAirborne;
    }
  }
}