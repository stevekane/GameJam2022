using UnityEngine;

public class TriggerEvent : MonoBehaviour {
  public EventSource<Collider> OnTriggerEnterSource = new();
  public EventSource<Collider> OnTriggerExitSource = new();
  public EventSource<Collider> OnTriggerStaySource = new();

  void OnTriggerEnter(Collider c) => OnTriggerEnterSource.Fire(c);
  void OnTriggerExit(Collider c) => OnTriggerExitSource.Fire(c);
  void OnTriggerStay(Collider c) => OnTriggerStaySource.Fire(c);
}