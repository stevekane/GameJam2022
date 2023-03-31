using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Time)]
public class FixedFrame : MonoBehaviour {
  public static FixedFrame Instance;

  public EventSource TickEvent = new();
  public int Tick { get; private set; }

  void Awake() => Instance = this;
  void OnDestroy() => Instance = null;
  void FixedUpdate() {
    Tick++;
    TickEvent.Fire();
    Timeval.TickCount = Tick;
    Timeval.TickEvent.Fire();
  }
}