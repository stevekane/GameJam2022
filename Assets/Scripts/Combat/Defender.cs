using UnityEngine;

public class Defender : MonoBehaviour {
  Status Status;
  Damage Damage;
  Pushable Pushable;
  Vibrator Vibrator;
  Animator Animator;
  AudioSource AudioSource;
  Attacker Attacker;
  Hurtbox[] Hurtboxes;

  public bool IsParrying; 
  public bool IsBlocking;

  public void OnParry(Attack attack) {
    Animator.SetTrigger("Parry");
  }

  public void OnBlock(Attack attack) {
    Animator.SetTrigger("Block");
  }

  // TODO: Remove all magic values
  public void OnHit(Attack attack) {
    var knockBackType = attack.Config.KnockBackType;
    var hitStopFrames = attack.Config.ContactDurationRuntime.Frames;
    var points = attack.Config.Points;
    var strength = attack.Config.Strength;
    var power = 5f * strength * Mathf.Pow((points+100f) / 100f, 2f);
    // TODO: how to handle projectiles?
    var knockBackDirection = AttackMelee.KnockbackVector(attack.Attacker.transform, transform, knockBackType);
    Status?.Add(new HitStunEffect(hitStopFrames, new KnockbackEffect(knockBackDirection*power)));
    Vibrator?.Vibrate(knockBackDirection, hitStopFrames, .15f);
    Damage?.AddPoints(points);
  }

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
    Pushable = GetComponent<Pushable>();
    Vibrator = GetComponent<Vibrator>();
    Animator = GetComponent<Animator>();
    AudioSource = GetComponent<AudioSource>();
    Attacker = GetComponent<Attacker>();
    Hurtboxes = GetComponentsInChildren<Hurtbox>();
    Hurtboxes.ForEach(b => b.Defender = this);
  }

  void OnDestroy() {
    Hurtboxes.ForEach(b => b.Defender = null);
  }

  void FixedUpdate() {
    var dt = Time.fixedDeltaTime;
  }
}