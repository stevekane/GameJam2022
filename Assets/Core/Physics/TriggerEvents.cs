using UnityEngine;
using UnityEngine.Events;

public class TriggerEvents : MonoBehaviour {
  public UnityEvent TriggerEnter;
  public UnityEvent TriggerStay;
  public UnityEvent TriggerExit;
  public LayerMask LayerMask;

  bool IsLayerInLayerMask(int layer, LayerMask layerMask) {
    return (layerMask.value & (1 << layer)) != 0;
  }

  bool Satisfied(Collider c) => IsLayerInLayerMask(c.gameObject.layer, LayerMask);

  void OnTriggerEnter(Collider c) {
    if (Satisfied(c)) {
      TriggerEnter?.Invoke();
    }
  }

  void OnTriggerStay(Collider c) {
    if (Satisfied(c)) {
      TriggerStay?.Invoke();
    }
  }

  void OnTriggerExit(Collider c) {
    if (Satisfied(c)) {
      TriggerExit?.Invoke();
    }
  }
}