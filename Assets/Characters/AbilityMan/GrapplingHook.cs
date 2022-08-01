using UnityEngine;

public class GrapplingHook : MonoBehaviour {
  public GameObject Owner;
  public Transform Origin;
  public EventSource<Collision> OnHit = new();

  void OnCollisionEnter(Collision c) {
    if (c.transform.gameObject != Owner) {
      Debug.Log($"You hit {c.transform.name}");
      OnHit.Action?.Invoke(c);
    }
  }

  void LateUpdate() {
    var lr = GetComponent<LineRenderer>();
    lr.SetPosition(0, transform.position);
    lr.SetPosition(1, Origin.position);
  }
}