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

  public void OnParry(Attacker attacker) {
    Animator.SetTrigger("Parry");
  }

  public void OnBlock(Attacker attacker) {
    Animator.SetTrigger("Block");
  }

  // TODO: Remove all magic values
  public void OnHit(Attacker attacker) {
    var knockBackType = attacker.Attack.Config.KnockBackType;
    var hitStopFrames = attacker.Attack.Config.ContactDurationRuntime.Frames;
    var points = attacker.Attack.Config.Points;
    var strength = attacker.Attack.Config.Strength;
    var power = 5f * strength * Mathf.Pow((points+100f) / 100f, 2f);
    var knockBackDirection = Attack.KnockbackVector(attacker.transform, transform, knockBackType);
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