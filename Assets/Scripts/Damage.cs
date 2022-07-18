using UnityEngine;

public class Damage : MonoBehaviour {
  public float Points = 0;

  Status Status;
  Vibrator Vibrator;

  // TODO: Tempted to move this to an interface implemented directly by actual
  // things in the world (like Vapor, Villain, Hero, etc)
  // This would allow them to respond to being hit directly and cut out this somewhat
  // strange indirection through the Damage component
  public Attack.Contact TakeDamage(Vector3 dir, int hitStopFrames, float points, float strength) {
    // TODO: velocity and duration are arbitrary
    var power = 5f * strength * Mathf.Pow((Points+100f) / 100f, 2f);
    Status.Add(new HitStunEffect(hitStopFrames, new KnockbackEffect(dir*power)));
    Vibrator?.Vibrate(transform.forward, hitStopFrames, .15f);
    Points += points;
    return Attack.Contact.Hit;
  }

  void Awake() {
    Status = GetComponent<Status>();
    Vibrator = GetComponent<Vibrator>();
  }
}