using System.Collections.Generic;
using UnityEngine;

public enum AttackState { None, Windup, Active, Contact, Recovery }

public class Attacker : MonoBehaviour {
  [SerializeField] Pushable Pushable;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] Animator Animator;
  [SerializeField] Attack[] Attacks;

  List<Hurtbox> Hits = new List<Hurtbox>(32);
  AttackState State;
  Attack Attack;
  Vector3 TotalKnockbackVector;
  float TotalKnockBackStrength;
  int FramesRemaining = 0;

  public bool IsAttacking { get { return State != AttackState.None; } }
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

  public void StartAttack(int index) {
    Attack = Attacks[index];
    State = AttackState.Windup;
    FramesRemaining = Attack.Config.Windup.Frames.ScaleBy(1 / Attack.Config.WindupAnimationSpeed);
    Attack.AudioSource.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    TrySpawnEffect(Attack.Config.WindupEffect, transform.position);
  }

  public void Hit(Hurtbox hurtbox) {
    Hits.Add(hurtbox);
  }

  public void Step(float dt) {
    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = Attack.Config.Active.Frames.ScaleBy(1 / Attack.Config.ActiveAnimationSpeed);
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
      TrySpawnEffect(Attack.Config.ActiveEffect, transform.position);
    } else if (State == AttackState.Active && Hits.Count > 0) {
      State = AttackState.Contact;
      FramesRemaining = Attack.Config.Contact.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.HitAudioClip);
      Vibrator.Vibrate(transform.forward, Attack.Config.Contact.Frames, .15f);
      CameraShaker.Instance.Shake(Attack.Config.HitCameraShakeIntensity);
      Hits.ForEach(OnHit);
      TotalKnockBackStrength = Attack.Config.Strength;
      TotalKnockbackVector = -Hits.Sum(hit => KnockbackVector(transform, hit.transform, Attack.Config.KnockBackType));
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames.ScaleBy(1 / Attack.Config.RecoveryAnimationSpeed);
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      TrySpawnEffect(Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Contact && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames.ScaleBy(1 / Attack.Config.RecoveryAnimationSpeed);
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      Pushable.Push(TotalKnockBackStrength * TotalKnockbackVector.normalized);
      TrySpawnEffect(Attack.Config.RecoveryEffect, transform.position);
    } else if (State == AttackState.Recovery && FramesRemaining <= 0) {
      Attack = null;
      State = AttackState.None;
      FramesRemaining = 0;
    }

    Attacks.ForEach(a => a.HitBox.Collider.enabled = a == Attack && State == AttackState.Active);
    FramesRemaining = Mathf.Max(0, FramesRemaining - 1);
    Animator.SetInteger("AttackState", (int)State);
    Animator.SetFloat("AttackIndex", Attack ? Attack.Config.Index : 0);
    Animator.SetFloat("AttackSpeed", AttackSpeed(Attack, State));
    Hits.Clear();
  }

  void OnHit(Hurtbox hit) {
    var direction = KnockbackVector(transform, hit.transform, Attack.Config.KnockBackType);
    var hitStopFrames = Attack.Config.Contact.Frames;
    var points = Attack.Config.Points;
    var strength = Attack.Config.Strength;
    hit.Damage?.TakeDamage(direction, hitStopFrames, points, strength);
    TrySpawnEffect(Attack.Config.ContactEffect, hit.transform.position);
  }

  float AttackSpeed(Attack a, AttackState s) {
    return s switch {
      AttackState.None => 1,
      AttackState.Windup => a.Config.WindupAnimationSpeed,
      AttackState.Active => a.Config.ActiveAnimationSpeed,
      AttackState.Recovery => a.Config.RecoveryAnimationSpeed,
      AttackState.Contact => 0,
      _ => 1,
    };
  }

  Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    return type switch {
      KnockBackType.Delta => p0.TryGetDirection(p1).OrDefault(attacker.forward),
      KnockBackType.Forward => attacker.forward,
      KnockBackType.Back => -attacker.forward,
      KnockBackType.Right => attacker.right,
      KnockBackType.Left => -attacker.right,
      KnockBackType.Up => attacker.up,
      KnockBackType.Down => -attacker.up,
      _ => attacker.forward,
    };
  }

  public static bool TrySpawnEffect(GameObject prefab, Vector3 position) {
    if (prefab) {
      var rotation = Quaternion.identity;
      var effect = Instantiate(prefab, position, rotation);
      effect.transform.localScale = new Vector3(10, 10, 10);
      effect.transform.LookAt(MainCamera.Instance.transform.position);
      Destroy(effect, 3);
      return true;
    } else {
      return false;
    }
  }
}