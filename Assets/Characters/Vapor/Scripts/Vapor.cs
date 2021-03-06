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

  AbilityUser Abilities;
  Defender Defender;
  Cannon Cannon;
  Status Status;
  CharacterController Controller;
  Animator Animator;
  AudioSource AudioSource;

  Ability CurrentAbility;
  Wire Wire;
  int WireFramesTraveled;
  Motion Motion;
  Vector3 Velocity;
  int PunchCycleIndex;

  bool IsAttacking { get => CurrentAbility != null; }

  public void RideWire(Wire wire) {
    Wire = wire;
  }

  void Awake() {
    Abilities = GetComponent<AbilityUser>();
    Defender = GetComponent<Defender>();
    Cannon = GetComponentInChildren<Cannon>();
    Status = GetComponent<Status>();
    Controller = GetComponent<CharacterController>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    var action = Inputs.Action;

    if (!CurrentAbility?.IsRunning ?? false)
      CurrentAbility = null;

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

      if (action.R1.JustDown && !IsAttacking) {
        CurrentAbility = Abilities.TryStartAbility(0+PunchCycleIndex);
        //Attacker.StartAttack(0+PunchCycleIndex);
        PunchCycleIndex = PunchCycleIndex <= 0 ? 1 : 0;
      } else if (action.R2.JustDown && !IsAttacking) {
        CurrentAbility = Abilities.TryStartAbility(2);
        // TODO: charge
      } else if (action.R2.JustUp && IsAttacking) {
        //Attacker.ReleaseChargeAttack();
      }

      // TODO: recoil
      //if (Cannon.IsFiring) {
      //  Pushable.Push(FIRING_PUSHBACK_SPEED*-transform.forward);
      //}
    }

    Animator.SetBool("Dashing", Motion == Motion.Dashing);
    Animator.SetBool("WireRiding", Motion == Motion.WireRiding);

    if (Motion == Motion.Base) {
      var moveSpeed = 0 switch {
        _ when !Status.CanMove => 0,
        //_ when IsAttacking => Attacker.MoveFactor*MOVE_SPEED, // TODO
        _ when IsAttacking => .5f*MOVE_SPEED,
        _ when Cannon.IsFiring => FIRING_MOVE_SPEED,
        _ => MOVE_SPEED
      };
      var moveVelocity = VelocityFromMove(action, moveSpeed);
      var gravity = dt*GRAVITY;
      Velocity.SetXZ(moveVelocity);
      Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
      Controller.Move(dt*Velocity);
    } else if (Motion == Motion.Dashing) {
      var moveVelocity = VelocityFromMove(action, DASH_SPEED);
      Velocity.SetXZ(moveVelocity);
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

    var turnSpeed = 0 switch {
      _ when IsAttacking => ATTACKING_TURN_SPEED,
      _ when Cannon.IsFiring => FIRING_TURN_SPEED,
      _ => TURN_SPEED
    };
    transform.rotation = RotationFromInputs(transform, turnSpeed, action, dt);
  }
}