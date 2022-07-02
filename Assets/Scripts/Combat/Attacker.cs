using System.Collections.Generic;
using UnityEngine;

public enum AttackState { None, Windup, Active, Contact, Recovery }

public class Attacker : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] Attack[] Attacks;

  List<Hurtbox> Hits = new List<Hurtbox>(32);
  AttackState State;
  Attack Attack;
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

  public void Hit(Hurtbox hurtbox) {
    Hits.Add(hurtbox);
  }

  public void SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation) {
    var effect = Instantiate(Attack.Config.HitEffect,position,rotation);
    effect.transform.localScale = new Vector3(10,10,10);
    effect.transform.LookAt(MainCamera.Instance.transform.position);
    Destroy(effect,3);
  }

  public void StartAttack(int index) {
    Attack = Attacks[index];
    State = AttackState.Windup;
    FramesRemaining = Attack.Config.Windup.Frames;
    Attack.AudioSource.PlayOptionalOneShot(Attack.Config.WindupAudioClip);
  }

  public void Step(float dt) {
    if (State == AttackState.Windup && FramesRemaining <= 0) {
      State = AttackState.Active;
      FramesRemaining = Attack.Config.Active.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.ActiveAudioClip);
    } else if (State == AttackState.Active && Hits.Count > 0) {
      State = AttackState.Contact;
      FramesRemaining = Attack.Config.Contact.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.HitAudioClip);
      CameraShaker.Instance.Shake(Attack.Config.HitCameraShakeIntensity);
      foreach (var hit in Hits) {
        var direction = KnockbackVector(transform,hit.transform,Attack.Config.KnockBackType);
        var hitStopFrames = Attack.Config.Contact.Frames;
        var points = Attack.Config.Points;
        var strength = Attack.Config.Strength;
        hit.Damage?.TakeDamage(direction,hitStopFrames,points,strength);
        SpawnEffect(Attack.Config.HitEffect,hit.transform.position,Quaternion.identity);
      }
    } else if (State == AttackState.Active && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);
    } else if (State == AttackState.Contact && FramesRemaining <= 0) {
      State = AttackState.Recovery;
      FramesRemaining = Attack.Config.Recovery.Frames;
      Attack.AudioSource.PlayOptionalOneShot(Attack.Config.RecoveryAudioClip);;
    } else if (State == AttackState.Recovery && FramesRemaining <= 0) {
      Attack = null;
      State = AttackState.None;
      FramesRemaining = 0;
    }

    foreach (var attack in Attacks) {
      attack.HitBox.Collider.enabled = attack == Attack && State == AttackState.Active;
    }

    FramesRemaining = Mathf.Max(0,FramesRemaining-1);
    Animator.SetInteger("AttackState",(int)State);
    Animator.SetFloat("AttackIndex",Attack ? Attack.Config.Index : 0);
    Animator.SetFloat("AttackSpeed",AttackSpeed(Attack,State));
    Hits.Clear();
  }
}