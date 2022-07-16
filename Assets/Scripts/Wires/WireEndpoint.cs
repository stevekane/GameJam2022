using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WireEndpoint : MonoBehaviour {
  [SerializeField] Wire Wire;

  public void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out IWireRider rider)) {
      Wire.OnContactEnd(this, rider);
    }
  }
}