using System.Collections.Generic;
using UnityEngine;

public enum AttackState { None, Windup, Active, Contact, Recovery }

public class Attacker : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] Attack[] Attacks;

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

  List<Hurtbox> Hits = new List<Hurtbox>(32);
  AttackState State;
  Attack Attack;
  int FramesRemaining = 0;

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
        var delta = hit.transform.position-transform.position;
        var direction = delta.XZ().normalized;
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
      if (attack != Attack) {
        attack.HitBox.Collider.enabled = false;
      }
    }

    FramesRemaining = Mathf.Max(0,FramesRemaining-1);
    Animator.SetInteger("AttackState",(int)State);
    Animator.SetFloat("AttackIndex",Attack ? Attack.Config.Index : 0);
    if (Attack) {
      Attack.HitBox.Collider.enabled = State == AttackState.Active;
    }
    switch (State) {
      case AttackState.None:     Animator.SetFloat("AttackSpeed",1); break;
      case AttackState.Windup:   Animator.SetFloat("AttackSpeed",Attack.Config.WindupAnimationSpeed); break;
      case AttackState.Active:   Animator.SetFloat("AttackSpeed",Attack.Config.ActiveAnimationSpeed); break;
      case AttackState.Contact:  Animator.SetFloat("AttackSpeed",0); break;
      case AttackState.Recovery: Animator.SetFloat("AttackSpeed",Attack.Config.RecoveryAnimationSpeed); break;
    }

    Hits.Clear();
  }
}