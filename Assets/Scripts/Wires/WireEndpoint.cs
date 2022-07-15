using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WireEndpoint : MonoBehaviour {
  [SerializeField] Wire Wire;

  public void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out Vapor vapor)) {
      Wire.OnContactEnd(this, vapor);
    }
  }
}