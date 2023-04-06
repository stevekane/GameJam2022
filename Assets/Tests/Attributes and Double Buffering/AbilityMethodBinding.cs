using System;
using System.Collections.Generic;

[Serializable]
public class AbilityMethodBinding {
  public AbilityMethodReference Method;
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
  public InputManager InputManager;
  public AbilityManager AbilityManager;
  public SampleAction Action;
  public List<ButtonEvent> ConsumedButtonEvents = new();
  public void Update() {
    Action.Satisfied = AbilityManager.CanInvoke(Method.GetMethod());
  }
  public void Bind() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Listen(TryFire);
    Action.EventSource.Listen(Fire);
  }
  public void Unbind() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType).Unlisten(TryFire);
    Action.EventSource.Unlisten(Fire);
  }
  void TryFire() {
    if (Action.TryFire()) {
      ConsumedButtonEvents.ForEach(InputManager.Consume);
    }
  }
  void Fire() {
    AbilityManager.TryInvoke(Method.GetMethod());
  }
}