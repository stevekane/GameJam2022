using System;
using System.Collections.Generic;
using UnityEngine;

public class ButtonEvents {
  public string Name;
  public EventSource JustDown;
  public EventSource Down;
  public EventSource JustUp;
  public ButtonEvents(string name) {
    Name = name;
    JustDown = new();
    Down = new();
    JustUp = new();
  }
}

public enum ButtonPressType {
  JustDown,
  Down,
  JustUp
}

public enum ButtonCode {
  L1,
  L2,
  R1,
  R2,
}

public class InputManager : MonoBehaviour {
  public static InputManager Instance;

  public ButtonEvents R1 = new ButtonEvents("R1");
  public ButtonEvents R2 = new ButtonEvents("R2");
  public ButtonEvents L1 = new ButtonEvents("L1");
  public ButtonEvents L2 = new ButtonEvents("L2");
  public InputAction[] InputActions;
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  
  public EventSource ButtonEvent(ButtonCode code, ButtonPressType type) {
    if (!Buttons.TryGetValue((code, type), out EventSource evt)) {
      evt = new();
      Buttons.Add((code, type), evt);
    }
    return evt;
  }

  void Awake() {
    Instance = this;
    MP.AbilityManager.Instance = new();
  }

  void Update() {
    foreach (var it in Buttons) {
      BroadcastEvent(it.Key.Item1, it.Key.Item2, it.Value);
    }
    BroadcastEvents(R1);
    BroadcastEvents(R2);
    BroadcastEvents(L1);
    BroadcastEvents(L2);
    foreach (var i in InputActions) {
      Predicate<string> predicate = i.Trigger switch {
        InputActionTrigger.JustDown => Input.GetButtonDown,
        InputActionTrigger.Down => Input.GetButton,
        InputActionTrigger.JustUp => Input.GetButtonUp,
        _ => throw new Exception($"unknown enumeration value ${i.Trigger}")
      };
      if (predicate(i.Name))
        i.Fire();
    }
  }

  void BroadcastEvent(ButtonCode code, ButtonPressType type, EventSource evt) {
    Predicate<string> func = type switch {
      ButtonPressType.JustDown => Input.GetButtonDown,
      ButtonPressType.Down => Input.GetButton,
      ButtonPressType.JustUp => Input.GetButtonUp,
      _ => Input.GetButtonDown
    };
    var name = code switch {
      ButtonCode.L1 => "L1",
      ButtonCode.L2 => "L2",
      ButtonCode.R1 => "R1",
      ButtonCode.R2 => "R2",
      _ => "",
    };
    if (func(name))
      evt.Fire();
  }


  void BroadcastEvents(ButtonEvents events) {
    if (Input.GetButtonDown(events.Name)) {
      events.JustDown.Fire();
    }
    if (Input.GetButton(events.Name)) {
      events.Down.Fire();
    }
    if (Input.GetButtonUp(events.Name)) {
      events.JustUp.Fire();
    }
  }
}

namespace MP {
  public class AbilityManager {
    public static AbilityManager Instance;

    // TODO: some kind of disjoint union would be preferred
    Dictionary<string, (ButtonCode, ButtonPressType)> TagToButton;
    Dictionary<string, EventSource> TagToEvent;

    public void RegisterTag(string name, ButtonCode code, ButtonPressType type) {
      Debug.Assert(!TagToEvent.ContainsKey(name));
      TagToButton[name] = (code, type);
    }
    public void RegisterTag(string name, EventSource source) {
      Debug.Assert(!TagToButton.ContainsKey(name));
      TagToEvent[name] = source;
    }
    public EventSource GetEvent(string name) {
      if (TagToEvent.TryGetValue(name, out EventSource evt))
        return evt;
      if (TagToButton.TryGetValue(name, out var button))
        return InputManager.Instance.ButtonEvent(button.Item1, button.Item2);
      return null;
    }
  }
}