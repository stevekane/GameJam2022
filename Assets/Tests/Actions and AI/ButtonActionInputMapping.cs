using UnityEngine;

public class ButtonInputMapping : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] ButtonPressType ButtonPressType;
  [SerializeField] ActionEventSource Action;
  [SerializeField] bool ConsumeOnFire;

  InputManager InputManager;

  void Fire() {
    if (Action.TryFire() && ConsumeOnFire)
      InputManager.Consume(ButtonCode, ButtonPressType);
  }

  void Awake() {
    InputManager = GetComponentInParent<InputManager>();
  }

  void OnEnable() {
    InputManager
    ?.ButtonEvent(ButtonCode, ButtonPressType)
    .Listen(Fire);
  }

  void OnDisable() {
    InputManager
    ?.ButtonEvent(ButtonCode, ButtonPressType)
    .Unlisten(Fire);
  }
}