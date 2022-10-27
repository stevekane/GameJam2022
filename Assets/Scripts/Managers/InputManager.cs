using System;
using System.Collections.Generic;
using UnityEngine;

public enum ButtonPressType {
  JustDown,
  Down,
  JustUp
}

// TODO: Maybe don't need/want these? Maybe just use strings
public enum ButtonCode {
  L1,
  L2,
  R1,
  R2,
  North,
  East,
  South,
  West
}

// TODO: Maybe don't need/want these? Maybe just use strings
public enum AxisCode {
  AxisLeft,
  AxisRight
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

  bool InputEnabled = true;
  public bool UseMouseAndKeyboard = false;
  Player Player;
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  public AxisState AxisLeft = new();
  public AxisState AxisRight = new();
  public float StickDeadZone;

  public IEventSource ButtonEvent(ButtonCode code, ButtonPressType type) {
    if (!Buttons.TryGetValue((code, type), out EventSource evt))
      Buttons.Add((code, type), evt = new());
    return evt;
  }

  public AxisState Axis(AxisCode code) {
    return code switch {
      AxisCode.AxisLeft => AxisLeft,
      AxisCode.AxisRight => AxisRight,
      _ => null
    };
  }

  public void SetInputEnabled(bool value) {
    InputEnabled = value;
    if (!InputEnabled) {
      AxisLeft.Update(0, new());
      AxisRight.Update(0, new());
    }
  }

  void Awake() {
    Time.fixedDeltaTime = 1f / Timeval.FramesPerSecond;
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

  void BroadcastEvent(ButtonCode code, ButtonPressType type, IEventSource evt) {
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
      ButtonCode.North => "North",
      ButtonCode.East => "East",
      ButtonCode.South => "South",
      ButtonCode.West => "West",
      _ => "",
    };
    if (func(name))
      evt.Fire();
  }

  void Update() {
    if (!InputEnabled)
      return;
    foreach (var it in Buttons) {
      BroadcastEvent(it.Key.Item1, it.Key.Item2, it.Value);
    }
    if (UseMouseAndKeyboard) {
      AxisLeft.Update(StickDeadZone, GetAxisFromKeyboard());
      AxisRight.Update(StickDeadZone, GetAxisFromMouse());
    } else {
      AxisLeft.Update(StickDeadZone, GetAxisFromInput("LeftX", "LeftY"));
      AxisRight.Update(StickDeadZone, GetAxisFromInput("RightX", "RightY"));
    }

    if (Input.GetKeyDown(KeyCode.W))
      UseMouseAndKeyboard = true;
    CheckSaveLoad();
  }

  void FixedUpdate() {
    Player = Player ?? FindObjectOfType<Player>();
    Timeval.FrameCount++;
  }

  // TODO: Remove this testing junk.
  void CheckSaveLoad() {
    if (Input.GetKeyDown(KeyCode.LeftBracket))
      SaveData.SaveToFile();
    if (Input.GetKeyDown(KeyCode.RightBracket))
      SaveData.LoadFromFile();
  }
}