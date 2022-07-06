using UnityEngine;

public class Damage : MonoBehaviour {
  public float Points = 0;

  Status Status;
  Vibrator Vibrator;

  public void TakeDamage(Vector3 dir, int hitStopFrames, float points, float strength) {
    // TODO: velocity and duration are arbitrary
    var power = 5f * strength * Mathf.Pow((Points+100f) / 100f, 2f);
    Status.Add(new DelayedEffect(hitStopFrames, new KnockbackEffect(dir*power)));
    Vibrator?.Vibrate(transform.forward, hitStopFrames, .15f);
    Points += points;
  }

  void Awake() {
    Status = GetComponent<Status>();
    Vibrator = GetComponent<Vibrator>();
  }
}