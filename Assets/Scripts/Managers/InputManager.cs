using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
  West,
  Unbound // Dummy value to easily unbind buttons
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
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  public AxisState AxisLeft = new();
  public AxisState AxisRight = new();
  public float StickDeadZone;
  PlayerInputActions Controls;

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
    Controls = new();
    //Controls.devices = new[] { Gamepad.all[1] };

    // TODO: move
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
  }
  void OnEnable() => Controls.Enable();
  void OnDisable() => Controls.Disable();

  void BroadcastEvent(ButtonCode code, ButtonPressType type, IEventSource evt) {
    if (code == ButtonCode.Unbound) return;
    var action = code switch {
      ButtonCode.L1 => Controls.Player.L1,
      ButtonCode.L2 => Controls.Player.L2,
      ButtonCode.R1 => Controls.Player.R1,
      ButtonCode.R2 => Controls.Player.R2,
      ButtonCode.North => Controls.Player.North,
      ButtonCode.East => Controls.Player.East,
      ButtonCode.South => Controls.Player.South,
      ButtonCode.West => Controls.Player.West,
      _ => null,
    };
    Func<bool> func = type switch {
      ButtonPressType.JustDown => action.WasPressedThisFrame,
      ButtonPressType.Down => action.IsPressed,
      ButtonPressType.JustUp => action.WasReleasedThisFrame,
      _ => null
    };
    if (func())
      evt.Fire();
  }
  Vector2 GetAxisFromInput(InputAction action) {
    return action.ReadValue<Vector2>();
  }

  void Update() {
    if (!InputEnabled)
      return;
    foreach (var it in Buttons) {
      BroadcastEvent(it.Key.Item1, it.Key.Item2, it.Value);
    }
    AxisLeft.Update(StickDeadZone, GetAxisFromInput(Controls.Player.Move));
    AxisRight.Update(StickDeadZone, GetAxisFromInput(Controls.Player.Look));

    CheckSaveLoad();
  }

  void FixedUpdate() {
    Timeval.TickCount++;
    Timeval.TickEvent.Fire();
  }

  // TODO: Remove this testing junk.
  void CheckSaveLoad() {
    if (Input.GetKeyDown(KeyCode.LeftBracket))
      SaveData.SaveToFile();
    if (Input.GetKeyDown(KeyCode.RightBracket))
      SaveData.LoadFromFile();
  }
}