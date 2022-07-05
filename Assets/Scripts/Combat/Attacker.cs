using System.Collections.Generic;
using UnityEngine;

public enum AttackState { None, Windup, Active, Contact, Recovery }

/*
I'd like to have a "smart targeting" system for attacks.

The goal is to help convert raw inputs into player intention in such a way 
that they have the feeling of selecting new things to attack by pointing at them
but also that when they behave "as though they are trying to perform a combo"
that they should become "stuck" to the target they have most recently engaged.

TARGET SELECTION

AIM ASSIST
  Map raw player intended direction to adjusted direction.
    "Point at best target"
  Map raw player movement to adjusted movement.
    "Don't run through a target if already well in-range"
*/

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
      switch (State) {
        case AttackState.Windup: return Attack.Config.WindupMoveFactor;
        case AttackState.Active: return Attack.Config.ActiveMoveFactor;
        case AttackState.Contact: return Attack.Config.ContactMoveFactor;
        case AttackState.Recovery: return Attack.Config.RecoveryMoveFactor;
        default: return 1;
      }
    }
  }
  public float RotationSpeed { 
    get { 
      switch (State) {
        case AttackState.Windup: return Attack.Config.WindupRotationDegreesPerSecond;
        case AttackState.Active: return Attack.Config.ActiveRotationDegreesPerSecond;
        case AttackState.Contact: return Attack.Config.ContactRotationDegreesPerSecond;
        case AttackState.Recovery: return Attack.Config.RecoveryRotationDegreesPerSecond;
        default: return 360;
      }
    }
  }

  public void StartAttack(int index) {
    Attack = Attacks[index];
    State = AttackState.Windup;
    FramesRemaining = Attack.Config.Windup.Frames;
    Attack.AudioSource.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
    TrySpawnEffect(Attack.Config.WindupEffect,transform.position);
  }

  public void Hit(Hurtbox hurtbox) {
    Hits.Add(hurtbox);
  }

  public void Step(float dt) {
    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = Attack.Config.Active.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
      TrySpawnEffect(Attack.Config.ActiveEffect,transform.position);
    } else if (State == AttackState.Active && Hits.Count > 0) {
      State = AttackState.Contact;
      FramesRemaining = Attack.Config.Contact.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.HitAudioClip);
      Vibrator.Vibrate(transform.forward,Attack.Config.Contact.Frames,.15f);
      CameraShaker.Instance.Shake(Attack.Config.HitCameraShakeIntensity);
      Hits.ForEach(OnHit);
      TotalKnockBackStrength = Attack.Config.Strength;
      TotalKnockbackVector = -Hits.Sum(hit => KnockbackVector(transform,hit.transform,Attack.Config.KnockBackType));
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      TrySpawnEffect(Attack.Config.RecoveryEffect,transform.position);
    } else if (State == AttackState.Contact && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
      Pushable.Push(TotalKnockBackStrength*TotalKnockbackVector.normalized);
      TrySpawnEffect(Attack.Config.RecoveryEffect,transform.position);
    } else if (State == AttackState.Recovery && FramesRemaining <= 0) {
      Attack = null;
      State = AttackState.None;
      FramesRemaining = 0;
    }

    Attacks.ForEach(a => a.HitBox.Collider.enabled = a == Attack && State == AttackState.Active);
    FramesRemaining = Mathf.Max(0,FramesRemaining-1);
    Animator.SetInteger("AttackState",(int)State);
    Animator.SetFloat("AttackIndex",Attack ? Attack.Config.Index : 0);
    Animator.SetFloat("AttackSpeed",AttackSpeed(Attack,State));
    Hits.Clear();
  }

  /*
  HitStop(duration)
    animator playback speed slowed
    vibration begins
  HurtStop(duration)
    animator playback speed begins ramp to 0
    vibration begins
  DealDamage(amount)
  SpawnEffects(effects)
  PlaySounds(sounds)
  EndHitStop
    Knockback
  EndHurtStop
    Knockback
  */

  void OnHit(Hurtbox hit) {
    var direction = KnockbackVector(transform,hit.transform,Attack.Config.KnockBackType);
    var hitStopFrames = Attack.Config.Contact.Frames;
    var points = Attack.Config.Points;
    var strength = Attack.Config.Strength;
    hit.Damage?.TakeDamage(direction,hitStopFrames,points,strength);
    TrySpawnEffect(Attack.Config.ContactEffect,hit.transform.position);
  }

  float AttackSpeed(Attack a, AttackState s) {
    switch (s) {
      case AttackState.None: return 1;
      case AttackState.Windup: return a.Config.WindupAnimationSpeed;
      case AttackState.Active: return a.Config.ActiveAnimationSpeed;
      case AttackState.Contact: return a.Config.ContactAnimationSpeed;
      case AttackState.Recovery: return a.Config.RecoveryAnimationSpeed;
      default: return 1;
    }
  }

  Vector3 KnockbackVector(Transform attacker, Transform target, KnockBackType type) {
    var p0 = attacker.position.XZ();
    var p1 = target.position.XZ();
    switch (type) {
      case KnockBackType.Delta: return p0.TryGetDirection(p1).OrDefault(attacker.forward);
      case KnockBackType.Forward: return attacker.forward;
      case KnockBackType.Back: return -attacker.forward;
      case KnockBackType.Right: return attacker.right;
      case KnockBackType.Left: return -attacker.right;
      case KnockBackType.Up: return attacker.up;
      case KnockBackType.Down: return -attacker.up;
      default: return attacker.forward;
    }
  }

  bool TrySpawnEffect(GameObject prefab, Vector3 position) {
    if (prefab) {
      var rotation = Quaternion.identity;
      var effect = Instantiate(prefab,position,rotation);
      effect.transform.localScale = new Vector3(10,10,10);
      effect.transform.LookAt(MainCamera.Instance.transform.position);
      Destroy(effect,3);
      return true;
    } else {
      return false;
    }
  }
}