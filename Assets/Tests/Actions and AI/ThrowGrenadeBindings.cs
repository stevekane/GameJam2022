using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionsAndAI {
  public class ThrowGrenadeBindings : MonoBehaviour {
    [SerializeField] InputAction Throw;
    [SerializeField] ActionEventSource ThrowAction;

    void Awake() => Throw.performed += ctx => ThrowAction.Fire();
    void OnEnable() => Throw.Enable();
    void OnDisable() => Throw.Disable();
    void FixedUpdate() => Throw.SetEnabled(ThrowAction.IsAvailable);
  }
}