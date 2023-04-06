using UnityEngine;
using UnityEngine.Events;

public class SampleAction : MonoBehaviour {
  public UnityEvent Event = new();
  public EventSource EventSource = new();
  [field:SerializeField]
  public virtual bool Satisfied { get; set; }
  public void Fire() {
    Event.Invoke();
    EventSource.Fire();
  }
  public bool TryFire() {
    if (Satisfied) {
      Fire();
      return true;
    } else {
      return false;
    }
  }
}