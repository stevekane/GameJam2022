using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Early)]
  public class PlayerController : MonoBehaviour {
    [SerializeField] ActionManager ActionManager;
    [SerializeField] InputManager InputManager;

    void FixedUpdate() {
      InputManager.ClearAllAxisEvents();
      InputManager.ClearAllButtonEvents();
      foreach (var action in ActionManager.Actions) {
        if (action.CanStart() && action.TryGetComponent(out ActionInputMapping mapping)) {
          InputManager.ButtonEvent(mapping.ButtonCode, mapping.ButtonPressType).Set(delegate {
            InputManager.Consume(mapping.ButtonCode, mapping.ButtonPressType);
            action.OnStart();
          });
        }
      }
      foreach (var action in ActionManager.AxisActions) {
        if (action.CanStart() && action.TryGetComponent(out AxisActionInputMapping mapping)) {
          InputManager.AxisEvent(mapping.AxisCode).Set(action.OnStart);
        }
      }
    }
  }
}