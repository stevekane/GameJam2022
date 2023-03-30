using System;
using UnityEngine;

namespace ActionsAndAI {
  [Serializable]
  public class ActionEventSource : MonoBehaviour, IEventSource {
    [field:SerializeField]
    public bool IsAvailable { get; set; }
    public Action Action;
    public void Listen(Action handler) => Action += handler;
    public void Set(Action handler) => Action = handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Clear() => Action = default;
    public void Fire() {
      if (IsAvailable)
        Action?.Invoke();
    }
  }
}