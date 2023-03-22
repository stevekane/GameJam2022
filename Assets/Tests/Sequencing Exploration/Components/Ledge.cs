using UnityEngine;

public class Ledge : MonoBehaviour {
  public Transform Pivot;

  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out LedgeGrabber ledgeGrabber)) {
      ledgeGrabber.Owner.SendMessage("OnLedgeEnter", this);
    }
  }

  void OnTriggerStay(Collider c) {
    if (c.TryGetComponent(out LedgeGrabber ledgeGrabber)) {
      ledgeGrabber.Owner.SendMessage("OnLedgeStay", this);
    }
  }

  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out LedgeGrabber ledgeGrabber)) {
      ledgeGrabber.Owner.SendMessage("OnLedgeExit", this);
    }
  }
}