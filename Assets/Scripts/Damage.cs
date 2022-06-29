using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage : MonoBehaviour {
  public static float StrengthLight = 1f;
  public static float StrengthHeavy = 2.5f;
  public static float StrengthSmash = 5f;

  public float Points = 0;
  Status Status;

  public void TakeDamage(Vector3 dir, float points, float strength) {
    // TODO: velocity and duration are arbitrary
    var power = 5f * strength * (Points+10f) / 100f;
    Status.Add(new KnockbackEffect(dir*power*power, GetDuration(power)));
    Points += points;
  }

  Timeval GetDuration(float power) {
    return Timeval.FromMillis((int)(power * 100f));
  }

  private void Awake() {
    Status = GetComponent<Status>();
  }
}
