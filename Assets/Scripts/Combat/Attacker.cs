using UnityEngine;

public enum AttackState { 
  None, 
  Windup, 
  Active, 
  Recovery,
  Parried
}

public class Attacker : MonoBehaviour {
  Status Status;
  Damage Damage;
  Pushable Pushable;
  Vibrator Vibrator;
  AudioSource AudioSource;
  Attack[] Attacks;
  Defender Defender;

  AttackState State;
  Vector3 TotalKnockbackVector;  // TODO: rename PushVector
  float TotalKnockBackStrength;
  int FramesRemaining;
  int ContactFramesRemaining;
  bool IsCharging;
  bool IsHolding;
  bool InContact;

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
    Pushable = GetComponent<Pushable>();
    Vibrator = GetComponent<Vibrator>();
    AudioSource = GetComponent<AudioSource>();
    Defender = GetComponent<Defender>();
    Attacks = GetComponentsInChildren<Attack>();
    Attacks.ForEach(a => a.Attacker = this);
  }

  public Attack Attack { get; internal set; }
  public bool IsReady { get => State == AttackState.None; }
  public bool IsParried { get => State == AttackState.Parried; }
  public bool IsAttacking { get => State != AttackState.None && State != AttackState.Parried; }
  public int AttackIndex { get => Attack ? Attack.Config.Index : -1; }

  public float AttackSpeed {
    get {
      return State switch {
        AttackState.None => 1,
        AttackState.Windup => Attack.Config.WindupAnimationSpeed(IsCharging),
        AttackState.Active when (InContact || IsHolding) => 0,
        AttackState.Active when !(InContact || IsHolding) => Attack.Config.ActiveAnimationSpeed,
        AttackState.Recovery => Attack.Config.RecoveryAnimationSpeed,
        AttackState.Parried => 1,
        _ => 1,
      };
    }
  }
  public AttackState AttackState {
    get { return State; }
  }
  public float MoveFactor {
    get {
      return State switch {
        AttackState.Windup => Attack.Config.WindupMoveFactor,
        AttackState.Active when InContact => 0,
        AttackState.Active when !InContact => Attack.Config.ActiveMoveFactor,
        AttackState.Recovery => Attack.Config.RecoveryMoveFactor,
        AttackState.Parried => 0,
        _ => 1,
      };
    }
  }

  public float RotationSpeed {
    get {
      return State switch {
        AttackState.Windup => Attack.Config.WindupRotationDegreesPerSecond,
        AttackState.Active when InContact => 0,
        AttackState.Active when !InContact => Attack.Config.ActiveRotationDegreesPerSecond,
        AttackState.Recovery => Attack.Config.RecoveryRotationDegreesPerSecond,
        AttackState.Parried => 0,
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

  public void ReleaseHoldAttack() {
    if (IsHolding && State == AttackState.Active) {
      FramesRemaining = Attack.Config.ActiveDurationRuntime.Frames;
    }
    IsHolding = false;
  }

  public void StartAttack(int index) {
    StartAttack(Attacks[index]);
  }

  public void StartChargeAttack(int index) {
    StartChargeAttack(Attacks[index]);
  }

  public void StartHoldAttack(int index) {
    IsHolding = true;
    StartAttack(Attacks[index]);
  }

  public void CancelAttack() {
    State = AttackState.None;
    FramesRemaining = 0;
    IsHolding = false;
  }

  public void OnBlock(Attack attack, Defender defender) {
    VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, attack.Config.ContactEffect, defender.transform.position);
  }

  public void OnParry(Attack attack, Defender defender) {
    VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, attack.Config.ContactEffect, defender.transform.position);
  }

  public void OnHit(Attack attack, Defender defender) {
    var direction = AttackMelee.KnockbackVector(transform, defender.transform, attack.Config.KnockBackType);
    var hitStopFrames = attack.Config.ContactDurationRuntime.Frames;
    var points = attack.Config.Points;
    var strength = attack.Config.Strength;
    if (attack is AttackMelee) {
      InContact = true;
      ContactFramesRemaining = attack.Config.ContactDurationRuntime.Frames;
      TotalKnockBackStrength = attack.Config.Strength;
      TotalKnockbackVector += AttackMelee.KnockbackVector(transform, defender.transform, attack.Config.KnockBackType);
      AudioSource?.PlayOptionalOneShot(attack.Config.HitAudioClip);
      Vibrator?.Vibrate(transform.forward, attack.Config.ContactDurationRuntime.Frames, .15f);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, attack.Config.ContactEffect, defender.transform.position);
      CameraShaker.Instance?.Shake(attack.Config.HitCameraShakeIntensity);
    } else {
      AudioSource?.PlayOptionalOneShot(attack.Config.HitAudioClip);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, attack.Config.ContactEffect, defender.transform.position);
    }
  }

  void FixedUpdate() {
    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = IsHolding ? int.MaxValue : Attack.Config.ActiveDurationRuntime.Frames;
      AudioSource?.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.ActiveEffect, transform.position);
      (Attack as AttackRanged)?.EnterActive();
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.RecoveryDurationRuntime.Frames;
      TotalKnockBackStrength = 0;
      TotalKnockbackVector = Vector3.zero;
      (Attack as AttackRanged)?.ExitActive();
      Pushable?.Push(TotalKnockBackStrength*TotalKnockbackVector.normalized);
      AudioSource?.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      VFXManager.Instance?.TrySpawnEffect(MainCamera.Instance, Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Recovery && FramesRemaining <= 0) {
      Attack = null;
      State = AttackState.None;
      FramesRemaining = 0;
    }

    if (State == AttackState.Active && InContact && ContactFramesRemaining <= 0) {
      InContact = false;
    }

    Attacks.ForEach(a => a.gameObject.SetActive(a == Attack && State == AttackState.Active));
    FramesRemaining = State switch {
      AttackState.None => 0,
      AttackState.Active when InContact => FramesRemaining,
      _ => FramesRemaining-1
    };
    ContactFramesRemaining = State switch {
      AttackState.Active when InContact => ContactFramesRemaining-1,
      _ => 0
    };
  }
}