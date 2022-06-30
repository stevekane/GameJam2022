using UnityEngine;

public class Damage : MonoBehaviour {
  public float Points = 0;
  Status Status;

  public void TakeDamage(Vector3 dir, float points, float strength) {
    // TODO: velocity and duration are arbitrary
    var power = 5f * strength * Mathf.Pow((Points+100f) / 100f, 2f);
    Status.Add(new KnockbackEffect(dir*power));
    Points += points;
  }

  private void Awake() {
    Status = GetComponent<Status>();
  }
}