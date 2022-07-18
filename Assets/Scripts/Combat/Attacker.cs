using UnityEngine;

enum AttackState { None, Windup, Active, Contact, Recovery }

public class Attacker : MonoBehaviour {
  public static bool TrySpawnEffect(GameObject prefab, Vector3 position) {
    if (prefab) {
      var rotation = Quaternion.LookRotation(MainCamera.Instance.transform.position);
      var effect = Instantiate(prefab, position, rotation);
      Destroy(effect, 3);
      return true;
    } else {
      return false;
    }
  }

  [SerializeField] Pushable Pushable;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] int ChargeFrameMultiplier = 3;

  Attack[] Attacks;
  AttackState State;
  Attack Attack;
  Vector3 TotalKnockbackVector;
  float TotalKnockBackStrength;
  int FramesRemaining = 0;
  bool IsCharging;

  void Awake() {
    Attacks = GetComponentsInChildren<Attack>(includeInactive: false);
    Attacks.ForEach(a => a.Attacker = this);
  }

  void OnDestroy() {
    Attacks.ForEach(a => a.Attacker = null);
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

  public float AttackSpeed {
    get {
      return State switch {
        AttackState.None => 1f,
        AttackState.Windup => (IsCharging ? (1f/(float)ChargeFrameMultiplier) : 1) * Attack.Config.WindupAnimationSpeed,
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
    FramesRemaining = Attack.Config.Windup.Frames.ScaleBy(1/Attack.Config.WindupAnimationSpeed);
    AudioSource.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    TrySpawnEffect(Attack.Config.WindupEffect, transform.position);
  }

  public void StartChargeAttack(Attack attack) {
    Attack = attack;
    State = AttackState.Windup;
    IsCharging = true;
    FramesRemaining = ChargeFrameMultiplier*Attack.Config.Windup.Frames.ScaleBy(1/Attack.Config.WindupAnimationSpeed);
    AudioSource.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    TrySpawnEffect(Attack.Config.WindupEffect, transform.position);
  }

  public void ReleaseChargeAttack() {
    if (State == AttackState.Windup) {
      FramesRemaining = FramesRemaining/ChargeFrameMultiplier;
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
      var direction = KnockbackVector(transform, hurtbox.transform, Attack.Config.KnockBackType);
      var hitStopFrames = Attack.Config.Contact.Frames;
      var points = Attack.Config.Points;
      var strength = Attack.Config.Strength;
      hurtbox.Damage?.TakeDamage(direction, hitStopFrames, points, strength);
      State = AttackState.Contact;
      FramesRemaining = Attack.Config.Contact.Frames;
      TotalKnockBackStrength = Attack.Config.Strength;
      TotalKnockbackVector += KnockbackVector(transform, hurtbox.transform, Attack.Config.KnockBackType);

      TrySpawnEffect(Attack.Config.ContactEffect, hurtbox.transform.position);
      AudioSource.PlayOptionalOneShot(Attack.Config.HitAudioClip);
      Vibrator.Vibrate(transform.forward, Attack.Config.Contact.Frames, .15f);
      CameraShaker.Instance.Shake(Attack.Config.HitCameraShakeIntensity);
    }
  }

  public void Step(float dt) {
    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = Attack.Config.Active.Frames.ScaleBy(1 / Attack.Config.ActiveAnimationSpeed);
      AudioSource.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
      TrySpawnEffect(Attack.Config.ActiveEffect, transform.position);
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames.ScaleBy(1 / Attack.Config.RecoveryAnimationSpeed);
      AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      TrySpawnEffect(Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Contact && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames.ScaleBy(1 / Attack.Config.RecoveryAnimationSpeed);
      AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      Pushable.Push(TotalKnockBackStrength * TotalKnockbackVector.normalized);
      TrySpawnEffect(Attack.Config.RecoveryEffect, transform.position);
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

  Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    return type switch {
      KnockBackType.Delta => p0.TryGetDirection(p1) ?? attacker.forward,
      KnockBackType.Forward => attacker.forward,
      KnockBackType.Back => -attacker.forward,
      KnockBackType.Right => attacker.right,
      KnockBackType.Left => -attacker.right,
      KnockBackType.Up => attacker.up,
      KnockBackType.Down => -attacker.up,
      _ => attacker.forward,
    };
  }
}