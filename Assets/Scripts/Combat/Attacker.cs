using UnityEngine;

enum AttackState { None, Windup, Active, Contact, Recovery }

public class Attacker : MonoBehaviour {
  Status Status;
  Damage Damage;
  Pushable Pushable;
  Vibrator Vibrator;
  AudioSource AudioSource;
  Attack[] Attacks;
  Defender Defender;

  AttackState State;
  Attack Attack;
  Vector3 TotalKnockbackVector;
  float TotalKnockBackStrength;
  int FramesRemaining = 0;
  bool IsCharging;

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
    Pushable = GetComponent<Pushable>();
    Vibrator = GetComponent<Vibrator>();
    AudioSource = GetComponent<AudioSource>();
    Defender = GetComponent<Defender>();
    Attacks = GetComponentsInChildren<Attack>();
    Attacks.ForEach(a => a.Hitbox.Attacker = this);
  }

  void OnDestroy() {
    Attacks.ForEach(a => a.Hitbox.Attacker = null);
  }

  public bool IsAttacking { 
    get { 
      return State != AttackState.None; 
    } 
  }

  public int AttackIndex {
    get {
      return Attack ? Attack.Config.Index : -1;
    }
  }

  public float WindupAttackSpeedMultiplier {
    get {
      return Attack && IsCharging ? 1f/Attack.Config.ChargeDurationMultiplier : 1f;
    }
  }

  public float AttackSpeed {
    get {
      return State switch {
        AttackState.None => 1f,
        AttackState.Windup => WindupAttackSpeedMultiplier*Attack.Config.WindupAnimationSpeed,
        AttackState.Active => Attack.Config.ActiveAnimationSpeed,
        AttackState.Recovery => Attack.Config.RecoveryAnimationSpeed,
        AttackState.Contact => 0f,
        _ => 1f,
      };
    }
  }

  public float MoveFactor {
    get {
      return State switch {
        AttackState.Windup => Attack.Config.WindupMoveFactor,
        AttackState.Active => Attack.Config.ActiveMoveFactor,
        AttackState.Recovery => Attack.Config.RecoveryMoveFactor,
        AttackState.Contact => 0,
        _ => 1,
      };
    }
  }

  public float RotationSpeed {
    get {
      return State switch {
        AttackState.Windup => Attack.Config.WindupRotationDegreesPerSecond,
        AttackState.Active => Attack.Config.ActiveRotationDegreesPerSecond,
        AttackState.Recovery => Attack.Config.RecoveryRotationDegreesPerSecond,
        AttackState.Contact => 0,
        _ => 360,
      };
    }
  }

  public void StartAttack(Attack attack) {
    Attack = attack;
    State = AttackState.Windup;
    IsCharging = false;
    FramesRemaining = Attack.Config.WindupDurationRuntime.Frames; 
    AudioSource?.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.WindupEffect, transform.position);
  }

  public void StartChargeAttack(Attack attack) {
    Attack = attack;
    State = AttackState.Windup;
    IsCharging = true;
    FramesRemaining = Attack.Config.ChargeDurationMultiplier*Attack.Config.WindupDurationRuntime.Frames;
    AudioSource?.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.WindupEffect, transform.position);
  }

  public void ReleaseChargeAttack() {
    if (State == AttackState.Windup) {
      FramesRemaining = FramesRemaining/Attack.Config.ChargeDurationMultiplier;
      IsCharging = false;
    }
  }

  public void StartAttack(int index) {
    StartAttack(Attacks[index]);
  }

  public void StartChargeAttack(int index) {
    StartChargeAttack(Attacks[index]);
  }

  public void CancelAttack() {
    State = AttackState.None;
    FramesRemaining = 0;
  }

  public void Hit(Hurtbox hurtbox) {
    if (State == AttackState.Active || State == AttackState.Contact) {
      var direction = Attack.KnockbackVector(transform, hurtbox.transform, Attack.Config.KnockBackType);
      var hitStopFrames = Attack.Config.ContactDurationRuntime.Frames;
      var points = Attack.Config.Points;
      var strength = Attack.Config.Strength;
      var contact = hurtbox.Defender.ResolveAttack(this, Attack);
      // TODO: Use contact to determine what effects / reaction should be
      State = AttackState.Contact;
      FramesRemaining = Attack.Config.ContactDurationRuntime.Frames;
      TotalKnockBackStrength = Attack.Config.Strength;
      TotalKnockbackVector += Attack.KnockbackVector(transform, hurtbox.transform, Attack.Config.KnockBackType);

      AudioSource?.PlayOptionalOneShot(Attack.Config.HitAudioClip);
      Vibrator?.Vibrate(transform.forward, Attack.Config.ContactDurationRuntime.Frames, .15f);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.ContactEffect, hurtbox.transform.position);
      CameraShaker.Instance?.Shake(Attack.Config.HitCameraShakeIntensity);
    }
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;

    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = Attack.Config.ActiveDurationRuntime.Frames;
      AudioSource?.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.ActiveEffect, transform.position);
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.RecoveryDurationRuntime.Frames; 
      AudioSource?.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Contact && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.RecoveryDurationRuntime.Frames; 
      AudioSource?.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      Pushable?.Push(TotalKnockBackStrength * TotalKnockbackVector.normalized);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Recovery && FramesRemaining <= 0) {
      Attack = null;
      State = AttackState.None;
      FramesRemaining = 0;
    }

    Attacks.ForEach(a => a.gameObject.SetActive(a == Attack && State == AttackState.Active));
    FramesRemaining = State == AttackState.None ? 0 : FramesRemaining-1;
    TotalKnockBackStrength = 0;
    TotalKnockbackVector = Vector3.zero;
  }
}