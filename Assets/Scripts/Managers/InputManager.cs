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

[Serializable]
public class AxisState {
  public Vector2 RawXY;
  public Vector2 XY;
  public Vector3 RawXZ;
  public Vector3 XZ;
  public void Update(float deadZone, Vector2 raw) {
    RawXY = raw;
    RawXZ = new Vector3(raw.x, 0, raw.y);
    if (raw.magnitude > deadZone) {
      XY = raw;
      XZ = RawXZ;
    } else {
      XY = Vector2.zero;
      XZ = Vector3.zero;
    }
  }
}

public class InputManager : MonoBehaviour {
  public static InputManager Instance;

  public bool UseMouseAndKeyboard = false;
  public ButtonEvents R1 = new ButtonEvents("R1");
  public ButtonEvents R2 = new ButtonEvents("R2");
  public ButtonEvents L1 = new ButtonEvents("L1");
  public ButtonEvents L2 = new ButtonEvents("L2");
  public InputAction[] InputActions;
  Player Player;
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  public AxisState AxisLeft = new();
  public AxisState AxisRight = new();

  public EventSource ButtonEvent(ButtonCode code, ButtonPressType type) {
    if (!Buttons.TryGetValue((code, type), out EventSource evt)) {
      evt = new();
      Buttons.Add((code, type), evt);
    }
    return evt;
  }

  void Awake() {
    Instance = this;
  }

  void Start() {
    Player = FindObjectOfType<Player>();
  }

  Vector2 GetAxisFromKeyboard() {
    float right = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
    float up = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
    return new Vector2(right, up);
  }

  Vector2 GetAxisFromMouse() {
    var playerPos = MainCamera.Instance.WorldToScreenPoint(Player.transform.position);
    var playerPos2 = new Vector2(playerPos.x, playerPos.y);
    var mousePos2 = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    return (mousePos2 - playerPos2).normalized;
  }
  Vector2 GetAxisFromInput(string xname, string yname) {
    return new Vector2(Input.GetAxisRaw(xname), Input.GetAxisRaw(yname));
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

  void Update() {
    foreach (var it in Buttons) {
      BroadcastEvent(it.Key.Item1, it.Key.Item2, it.Value);
    }
    if (UseMouseAndKeyboard) {
      AxisLeft.Update(0f, GetAxisFromKeyboard());
      AxisRight.Update(0f, GetAxisFromMouse());
    } else {
      AxisLeft.Update(0f, GetAxisFromInput("LeftX", "LeftY"));
      AxisRight.Update(0f, GetAxisFromInput("RightX", "RightY"));
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
