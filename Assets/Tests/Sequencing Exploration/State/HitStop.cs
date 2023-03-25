using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Early)]
public class HitStop : MonoBehaviour {
  [SerializeField] LocalTime LocalTime;

  public int TicksRemaining;

  void FixedUpdate() {
    TicksRemaining = Mathf.Max(TicksRemaining-1, 0);
    LocalTime.TimeScale = TicksRemaining <= 0 ? 1 : 0;
  }
}