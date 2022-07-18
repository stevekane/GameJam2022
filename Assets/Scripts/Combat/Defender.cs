using UnityEngine;

public class Defender : MonoBehaviour {
  Status Status;
  Damage Damage;
  Pushable Pushable;
  Vibrator Vibrator;
  AudioSource AudioSource;
  Attacker Attacker;
  Hurtbox[] Hurtboxes;

  // TODO: Remove all magic values
  public Attack.Contact ResolveAttack(Attacker attacker, Attack attack) {
    var knockBackType = attack.Config.KnockBackType;
    var hitStopFrames = attack.Config.Contact.Frames;
    var points = attack.Config.Points;
    var strength = attack.Config.Strength;
    var power = 5f * strength * Mathf.Pow((points+100f) / 100f, 2f);
    var knockBackDirection = Attack.KnockbackVector(attacker.transform, transform, knockBackType);
    Status?.Add(new HitStunEffect(hitStopFrames, new KnockbackEffect(knockBackDirection*power)));
    Vibrator?.Vibrate(knockBackDirection, hitStopFrames, .15f);
    Damage?.AddPoints(points);
    return Attack.Contact.Hit;
  }

  void Awake() {
    Status = GetComponent<Status>();
    Damage = GetComponent<Damage>();
    Pushable = GetComponent<Pushable>();
    Vibrator = GetComponent<Vibrator>();
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