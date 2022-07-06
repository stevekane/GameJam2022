using UnityEngine;

public class Damage : MonoBehaviour {
  public float Points = 0;

  Status Status;
  KnockbackEffect KnockbackEffect;
  int HitStopFramesRemaining;

  public void TakeDamage(Vector3 dir, int hitStopFrames, float points, float strength) {
    // TODO: velocity and duration are arbitrary
    var power = 5f * strength * Mathf.Pow((Points+100f) / 100f, 2f);
    KnockbackEffect = new KnockbackEffect(dir*power);
    HitStopFramesRemaining = hitStopFrames;
    Points += points;
  }

  void Awake() {
    Status = GetComponent<Status>();
  }

  void FixedUpdate() {
    HitStopFramesRemaining = Mathf.Max(0,HitStopFramesRemaining-1);
    if (HitStopFramesRemaining <= 0 && KnockbackEffect != null) {
      Status.Add(KnockbackEffect);
      KnockbackEffect = null;
    }
  }
}