using System;
using UnityEngine;

namespace ActionsAndAI {
  [Serializable]
  public class ActionEventSourceVector3 : MonoBehaviour, IEventSource<Vector3> {
    [field:SerializeField]
    public bool IsAvailable { get; set; }
    public Action<Vector3> Action;
    public void Listen(Action<Vector3> handler) => Action += handler;
    public void Set(Action<Vector3> handler) => Action = handler;
    public void Unlisten(Action<Vector3> handler) => Action -= handler;
    public void Clear() => Action = default;
    public void Fire(Vector3 t) {
      if (IsAvailable)
        Action?.Invoke(t);
    }
  }
}