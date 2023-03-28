using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Early)]
  public class PlayerController : MonoBehaviour {
    [SerializeField] ActionManager ActionManager;
    [SerializeField] InputManager InputManager;

    // TODO: ActionManager needs StickActions as well (with associated...abstract state and shit)

    void FixedUpdate() {
      // This is a rather stupid way to set the action.
      // It does not use any kind of priority but just
      // blindly walks the list always assigning the binding
      // this means last-writer wins atm
      // This is also horrible since we're using a delegate to wrap the onstart
      // call in order to consume the input.... should be made better
      foreach (var action in ActionManager.Actions) {
        if (action.CanStart()) {
          InputManager.ButtonEvent(action.ButtonCode, action.ButtonPressType).Set(delegate {
            InputManager.Consume(action.ButtonCode, action.ButtonPressType);
            action.OnStart();
          });
        }
      }
    }
  }
}