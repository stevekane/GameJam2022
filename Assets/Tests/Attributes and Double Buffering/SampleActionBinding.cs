using UnityEngine;

public class SampleActionBinding : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] ButtonPressType ButtonPressType;

  SampleAction Action;
  InputManager InputManager;

  void TryFire() {
    if (Action.Satisfied) {
      InputManager.Consume(ButtonCode, ButtonPressType);
      Action.Fire();
    }
  }

  void Start() {
    Action = GetComponent<SampleAction>();
    InputManager = GetComponentInParent<InputManager>();
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Listen(TryFire);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Unlisten(TryFire);
  }
}