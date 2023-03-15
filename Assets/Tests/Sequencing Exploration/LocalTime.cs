using UnityEngine;

public class LocalTime : MonoBehaviour {
  public float TimeScale = 1;
  public float FixedDeltaTime { get; private set; }
  public float DeltaTime { get; private set; }
  public float Time { get; private set; }

  void Update() {
    DeltaTime = TimeScale * UnityEngine.Time.timeScale * UnityEngine.Time.deltaTime;
    Time += DeltaTime;
  }

  void FixedUpdate() {
    FixedDeltaTime = TimeScale * UnityEngine.Time.timeScale * UnityEngine.Time.fixedDeltaTime;
  }

  void OnDrawGizmosSelected() {
    var center = transform.position + 2 * transform.up;
    var color = Color.Lerp(Color.red, Color.white, TimeScale);
    Gizmos.DrawIcon(center, "Clock.png", false, color);
  }
}