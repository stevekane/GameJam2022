using UnityEngine;

namespace ActionsAndAI {
  public class ThrowGrenade : MonoBehaviour {
    [SerializeField] ActionEventSource ThrowAction;

    void OnEnable() => ThrowAction.IsAvailable = true;
    void OnDisable() => ThrowAction.IsAvailable = false;
    void Throw() => Debug.Log("Throw");
  }
}