using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Time)]
public class FixedFrame : MonoBehaviour {
  public static FixedFrame Instance;

  public EventSource TickEvent = new();
  public int Tick { get; private set; }

  void Awake() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    Instance = this;
  }

  void OnDestroy() {
    Instance = null;
  }

  void FixedUpdate() {
    Tick++;
    TickEvent.Fire();
    Timeval.TickCount = Tick;
    Timeval.TickEvent.Fire();
  }
}