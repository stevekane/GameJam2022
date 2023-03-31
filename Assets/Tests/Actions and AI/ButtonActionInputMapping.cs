using UnityEngine;

public class ButtonInputMapping : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] ButtonPressType ButtonPressType;
  [SerializeField] ActionEventSource Action;
  void OnEnable() {
    GetComponentInParent<InputManager>()
    .ButtonEvent(ButtonCode, ButtonPressType)
    .Listen(Action.Fire);
  }
  void OnDisable() {
    GetComponentInParent<InputManager>()
    ?.ButtonEvent(ButtonCode, ButtonPressType)
    .Unlisten(Action.Fire);
  }
}