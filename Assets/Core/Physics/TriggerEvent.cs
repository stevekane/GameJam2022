using UnityEngine;

public class TriggerEvent : MonoBehaviour {
  public EventSource<Collider> OnTriggerEnterSource = new();
  public EventSource<Collider> OnTriggerExitSource = new();
  public EventSource<Collider> OnTriggerStaySource = new();
  Collider Collider;

  public bool EnableCollision {
    get => Collider.enabled;
    set => Collider.enabled = value;
  }

  void Awake() => Collider = GetComponent<Collider>();
  void OnTriggerEnter(Collider c) => OnTriggerEnterSource.Fire(c);
  void OnTriggerExit(Collider c) => OnTriggerExitSource.Fire(c);
  void OnTriggerStay(Collider c) => OnTriggerStaySource.Fire(c);
}