using UnityEngine;

public class GrappleHook : MonoBehaviour {
  public Grapple Grapple;

  public void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out GrappleTarget target)) {
      Grapple.Attach(target);
    }
  }
}