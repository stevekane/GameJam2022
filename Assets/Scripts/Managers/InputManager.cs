using System;
using System.Collections.Generic;
using UnityEngine;

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
  Player Player;
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  public AxisState AxisLeft = new();
  public AxisState AxisRight = new();

  public EventSource ButtonEvent(ButtonCode code, ButtonPressType type) {
    if (!Buttons.TryGetValue((code, type), out EventSource evt))
      Buttons.Add((code, type), evt = new());
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
  }
}
