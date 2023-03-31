using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ActionEventSource : MonoBehaviour {
  [SerializeField]
  public UnityEvent Action;
  [field:SerializeField]
  public bool IsAvailable { get; set; }
  public void Fire() {
    if (IsAvailable) {
      Action?.Invoke();
    }
  }
  public bool TryFire() {
    Fire();
    return IsAvailable;
  }
}