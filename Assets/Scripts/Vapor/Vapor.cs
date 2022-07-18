using UnityEngine;

enum Motion { Base, Dashing, WireRiding }

public class Vapor : MonoBehaviour, IWireRider {
  static Quaternion RotationFromInputs(Transform t, float speed, Action action, float dt) {
    var desiredForward = action.Right.XZ.TryGetDirection() ?? t.forward;
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = dt*speed;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  static Vector3 HeadingFromInputs(Transform t, Action action) {
    var heading = 
      action.Left.XZ.TryGetDirection() ??
      action.Right.XZ.TryGetDirection() ??
      t.forward;
    return heading;
  }

  static Vector3 VelocityFromMove(Action action, float speed) {
    return speed*action.Left.XZ;
  }

  [SerializeField] float GRAVITY;
  [SerializeField] float MOVE_SPEED;
  [SerializeField] float FIRING_MOVE_SPEED;
  [SerializeField] float DASH_SPEED;
  [SerializeField] float TURN_SPEED;
  [SerializeField] float FIRING_TURN_SPEED;
  [SerializeField] float ATTACKING_TURN_SPEED;
  [SerializeField] float FIRING_PUSHBACK_SPEED;
  [SerializeField] Timeval WireRide;
  [SerializeField] ParticleSystem ChargeParticles;
  [SerializeField] AudioClip ChargeAudioClip;
  [SerializeField] float ChargeAudioClipStartingTime;

  Attacker Attacker;
  Defender Defender;
  Cannon Cannon;
  Pushable Pushable;
  Status Status;
  CharacterController Controller;
  Animator Animator;
  AudioSource AudioSource;

  Wire Wire;
  int WireFramesTraveled;
  Motion Motion;
  Vector3 Velocity;
  int PunchCycleIndex;

  public void RideWire(Wire wire) {
    Wire = wire;
  }

  void Awake() {
    Attacker = GetComponent<Attacker>();
    Defender = GetComponent<Defender>();
    Cannon = GetComponentInChildren<Cannon>();
    Pushable = GetComponent<Pushable>();
    Status = GetComponent<Status>();
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;

    if (Status.CanMove && Motion == Motion.Base && action.L1.JustDown) {
      AudioSource.Stop();
      AudioSource.clip = ChargeAudioClip;
      AudioSource.time = ChargeAudioClipStartingTime;
      AudioSource.Play();
      Motion = Motion.Dashing;
    } else if (Motion == Motion.Dashing && action.L1.JustUp) {
      AudioSource.Stop();
      Motion = Motion.Base;
    } else if (Motion == Motion.Dashing && Wire) {
      Motion = Motion.WireRiding;
      WireFramesTraveled = 0;
    } else if (Motion == Motion.WireRiding && WireFramesTraveled >= WireRide.Frames) {
      if (action.L1.Down) {
        Motion = Motion.Dashing; 
        Wire = null;
      } else {
        AudioSource.Stop();
        Motion = Motion.Base;
        Wire = null;
      }
    }

    if (Status.CanAttack) {
      if (action.L2.JustDown) {
        Cannon.DepressTrigger();
      } else if (Status.CanAttack && action.L2.Down) {
        Cannon.HoldTrigger();
      } else if (action.L2.JustUp) {
        Cannon.ReleaseTrigger();
      }

      if (action.R1.JustDown && !Attacker.IsAttacking) {
        Attacker.StartAttack(0+PunchCycleIndex);
        PunchCycleIndex = PunchCycleIndex <= 0 ? 1 : 0;
      } else if (action.R2.JustDown && !Attacker.IsAttacking) {
        Attacker.StartChargeAttack(2+PunchCycleIndex);
        PunchCycleIndex = PunchCycleIndex <= 0 ? 1 : 0;
      } else if (action.R2.JustUp && Attacker.IsAttacking) {
        Attacker.ReleaseChargeAttack();
      }

      if (Cannon.IsFiring) {
        Pushable.Push(FIRING_PUSHBACK_SPEED*-transform.forward);
      }
    }

    Animator.SetBool("Dashing", Motion == Motion.Dashing);
    Animator.SetBool("WireRiding", Motion == Motion.WireRiding);
    Animator.SetBool("Attacking", Attacker.IsAttacking);
    Animator.SetInteger("AttackIndex", Attacker.AttackIndex);
    Animator.SetFloat("AttackSpeed", Attacker.AttackSpeed);

    if (Motion == Motion.Base) {
      var moveSpeed = MOVE_SPEED switch {
        _ when !Status.CanMove => 0,
        _ when Attacker.IsAttacking => Attacker.MoveFactor*MOVE_SPEED,
        _ when Cannon.IsFiring => FIRING_MOVE_SPEED,
        _ => MOVE_SPEED
      };
      var moveVelocity = VelocityFromMove(action, moveSpeed);
      var pushVelocity = Pushable.Impulse;
      var gravity = dt*GRAVITY;
      Velocity.x = moveVelocity.x;
      Velocity.z = moveVelocity.z;
      Velocity += pushVelocity;
      Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
      Controller.Move(dt*Velocity);
    } else if (Motion == Motion.Dashing) {
      var moveVelocity = VelocityFromMove(action, DASH_SPEED);
      Velocity.x = moveVelocity.x;
      Velocity.z = moveVelocity.z;
      Velocity.y = 0;
      ChargeParticles.transform.forward = -moveVelocity.TryGetDirection() ?? -transform.forward;
      Controller.Move(dt*Velocity);
    } else if (Motion == Motion.WireRiding) {
      var distance = 1f-(float)WireFramesTraveled/(float)WireRide.Frames;
      var wirePathData = Wire.Waypoints.ToWorldSpace(distance);
      var delta = wirePathData.Position-(transform.position);
      Velocity = delta/dt;
      Controller.Move(delta);
      WireFramesTraveled++;
    }
    
    var turnSpeed = TURN_SPEED switch {
      _ when Attacker.IsAttacking => ATTACKING_TURN_SPEED,
      _ when Cannon.IsFiring => FIRING_TURN_SPEED,
      _ => TURN_SPEED
    };
    transform.rotation = RotationFromInputs(transform, turnSpeed, action, dt);
  }
}