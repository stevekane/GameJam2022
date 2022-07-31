using UnityEngine;

public class GrapplingHook : MonoBehaviour {
  public Transform Origin;
  public EventSource<Collision> OnHit = new();

  void OnCollisionEnter(Collision c) {
    OnHit.Action?.Invoke(c);
  }

  void LateUpdate() {
    var lr = GetComponent<LineRenderer>();
    lr.SetPosition(0, transform.position);
    lr.SetPosition(1, Origin.position);
  }
}