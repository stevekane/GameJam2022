using System.Collections.Generic;
using UnityEngine;

public class ActionBinding : MonoBehaviour {
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] ButtonPressType ButtonPressType;
  [SerializeField] SampleAction Action;
  [SerializeField] List<ButtonEvent> ConsumedButtonEvents = new();

  InputManager InputManager;

  void TryFire() {
    if (Action.Satisfied) {
      InputManager.Consume(ButtonCode, ButtonPressType);
      ConsumedButtonEvents.ForEach(InputManager.Consume);
      Action.Fire();
    }
  }

  void Start() {
    InputManager = GetComponentInParent<InputManager>();
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Listen(TryFire);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Unlisten(TryFire);
  }
}