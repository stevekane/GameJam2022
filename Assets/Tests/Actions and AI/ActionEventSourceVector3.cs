using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ActionEventSourceVector3 : MonoBehaviour {
  [SerializeField]
  UnityEvent<Vector3> Action;
  [field:SerializeField]
  public bool IsAvailable { get; set; }
  public void Fire(Vector3 v) {
    if (IsAvailable)
      Action?.Invoke(v);
  }
  public bool TryFire(Vector3 v) {
    Fire(v);
    return IsAvailable;
  }
}