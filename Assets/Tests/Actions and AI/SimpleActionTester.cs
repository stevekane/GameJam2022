using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionsAndAI {
  public class SimpleActionTester : MonoBehaviour {
    [SerializeField] InputAction Aim;
    [SerializeField] InputAction AimStick;
    [SerializeField] ActionEventSource StartAimAction;
    [SerializeField] ActionEventSource StopAimAction;
    [SerializeField] ActionEventSourceVector3 UpdateAimAction;

    void Awake() {
      Aim.started += ctx => StartAimAction.Fire();
      Aim.canceled += ctx => StopAimAction.Fire();
      Aim.performed += ctx => StopAimAction.Fire();
      AimStick.performed += ctx => UpdateAimAction.Fire(ctx.ReadValue<Vector2>().XZ());
    }

    void OnEnable() {
      Aim.Enable();
      AimStick.Enable();
    }

    void OnDisable() {
      Aim.Disable();
      AimStick.Disable();
    }

    void FixedUpdate() {
      Aim.SetEnabled(StartAimAction.IsAvailable);
      AimStick.SetEnabled(UpdateAimAction.IsAvailable);
    }

    void OnDrawGizmos() {
      var log = "";
      if (Aim.enabled)
        log += "Aim\n";
      if (AimStick.enabled)
        log += "Update Aim";
      DebugUI.Log(this, log);
    }
  }
}