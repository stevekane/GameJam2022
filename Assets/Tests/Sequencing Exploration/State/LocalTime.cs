using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Time)]
public class LocalTime : MonoBehaviour {
  public float TimeScale = 1;
  public float DeltaTime => TimeScale * UnityEngine.Time.timeScale * UnityEngine.Time.deltaTime;
  public float FixedDeltaTime => TimeScale * UnityEngine.Time.timeScale * UnityEngine.Time.fixedDeltaTime;
  public float Time { get; private set; }
  public float FixedTime { get; private set; }

  void Update() {
    Time += DeltaTime;
  }

  void FixedUpdate() {
    FixedTime += FixedDeltaTime;
  }

  void OnDrawGizmosSelected() {
    var center = transform.position + 2 * transform.up;
    var color = Color.Lerp(Color.red, Color.white, TimeScale);
    Gizmos.DrawIcon(center, "Clock.png", false, color);
  }
}