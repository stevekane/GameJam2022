using UnityEngine;
using UnityEngine.Events;

public class SampleAction : MonoBehaviour {
  public UnityEvent Event = new();
  public EventSource EventSource = new();
  public virtual bool Satisfied { get; set; }
  public void Fire() {
    Event.Invoke();
    EventSource.Fire();
  }
}