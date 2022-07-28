using System;
using UnityEngine;

[Serializable]
public class HitParams {
  public Timeval HitStopDuration;
  public float Damage;
  public float KnockbackStrength;
  public KnockBackType KnockbackType;
}

public class Defender : MonoBehaviour {
  Status Status;
  Damage Damage;
  Pushable Pushable;
  Vibrator Vibrator;
  Animator Animator;
  AudioSource AudioSource;
  Attacker Attacker;

  public bool IsParrying; 
  public bool IsBlocking;

  public void OnParry(Attack attack) {
    if (Animator)
      Animator.SetTrigger("Parry");
  }

  public void OnBlock(Attack attack) {
    if (Animator)
      Animator.SetTrigger("Block");
  }

  // TODO: Remove all magic values
  public void OnHit(Attack attack) {
    var knockBackType = attack.Config.KnockBackType;
    var hitStopFrames = attack.Config.ContactDurationRuntime.Frames;
    var points = attack.Config.Points;
    var strength = attack.Config.Strength;
    var power = 5f * strength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    // TODO: how to handle projectiles?
    var knockBackDirection = AttackMelee.KnockbackVector(attack.Attacker.transform, transform, knockBackType);
    Status?.Add(new HitStunEffect(hitStopFrames), (s) => s.Add(new KnockbackEffect(knockBackDirection*power)));
    Vibrator?.Vibrate(knockBackDirection, hitStopFrames, .15f);
    Damage?.AddPoints(points);
  }

  public void OnHit(HitParams hit, Transform hitTransform) {
    var power = 5f * hit.KnockbackStrength * Mathf.Pow((Damage.Points+100f) / 100f, 2f);
    var hitStopFrames = hit.HitStopDuration.Frames;
    var knockBackDirection = AttackMelee.KnockbackVector(hitTransform, transform, hit.KnockbackType);
    Status?.Add(new HitStunEffect(hitStopFrames), (s) => s.Add(new KnockbackEffect(knockBackDirection*power)));
    Vibrator?.Vibrate(knockBackDirection, hitStopFrames, .15f);
    Damage?.AddPoints(hit.Damage);
  }

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
    Pushable = GetComponent<Pushable>();
    Vibrator = GetComponent<Vibrator>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
    Attacker = GetComponent<Attacker>();
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
    if (transform.position.y < -100f) {
      Destroy(gameObject, .01f);
    }
  }
}