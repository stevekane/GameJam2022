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

[DefaultExecutionOrder(ScriptExecutionGroups.Input)]
public class InputManager : MonoBehaviour {
  public Timeval BufferDuration = Timeval.FromAnimFrames(6,60);
  public float StickDeadZone;
  bool InputEnabled = true;
  Dictionary<(ButtonCode, ButtonPressType), int> Buffer = new();
  Dictionary<(ButtonCode, ButtonPressType), EventSource> Buttons = new();
  Dictionary<AxisCode, EventSource<AxisState>> Axes = new();
  AxisState AxisLeft = new();
  AxisState AxisRight = new();
  PlayerInputActions Controls;

  public IEventSource ButtonEvent(ButtonCode code, ButtonPressType type) {
    if (!Buttons.TryGetValue((code, type), out EventSource evt))
      Buttons.Add((code, type), evt = new());
    return evt;
  }

  public IEventSource<AxisState> AxisEvent(AxisCode code) {
    if (!Axes.TryGetValue(code, out EventSource<AxisState> evt))
      Axes.Add(code, evt = new());
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

  public void Consume(ButtonCode code, ButtonPressType type) {
    Buffer.Remove((code, type));
  }

  public void AssignDevices(InputDevice[] devices) {
    Controls.devices = devices;
  }

  public void ClearAllButtonEvents() {
    ClearButtonEvents(ButtonCode.L1);
    ClearButtonEvents(ButtonCode.L2);
    ClearButtonEvents(ButtonCode.R1);
    ClearButtonEvents(ButtonCode.R2);
    ClearButtonEvents(ButtonCode.North);
    ClearButtonEvents(ButtonCode.East);
    ClearButtonEvents(ButtonCode.South);
    ClearButtonEvents(ButtonCode.West);
  }

  public void ClearAllAxisEvents() {
    ClearAxisEvents(AxisCode.AxisLeft);
    ClearAxisEvents(AxisCode.AxisRight);
  }

  public void ClearButtonEvents(ButtonCode buttonCode) {
    ButtonEvent(buttonCode, ButtonPressType.JustDown).Clear();
    ButtonEvent(buttonCode, ButtonPressType.Down).Clear();
    ButtonEvent(buttonCode, ButtonPressType.JustUp).Clear();
  }

  public void ClearAxisEvents(AxisCode axisCode) {
    AxisEvent(axisCode).Clear();
  }

  void Awake() => Controls = new();
  void OnEnable() => Controls.Enable();
  void OnDisable() => Controls.Disable();

  void BroadcastButtonEvent(ButtonCode code, ButtonPressType type, IEventSource evt) {
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
      Buffer[(code, type)] = Timeval.TickCount;

    if (Buffer.TryGetValue((code, type), out int tickCount) && Timeval.TickCount - tickCount <= BufferDuration.Ticks)
      evt.Fire();
  }

  void BroadcastAxisEvent(AxisCode code, EventSource<AxisState> evt) {
    evt.Fire(code == AxisCode.AxisLeft ? AxisLeft : AxisRight);
  }

  Vector2 GetAxisFromInput(InputAction action) {
    return action.ReadValue<Vector2>();
  }

  void FixedUpdate() {
    if (!InputEnabled)
      return;
    AxisLeft.Update(StickDeadZone, GetAxisFromInput(Controls.Player.Move));
    AxisRight.Update(StickDeadZone, GetAxisFromInput(Controls.Player.Look));
    foreach (var it in Axes) {
      BroadcastAxisEvent(it.Key, it.Value);
    }
    foreach (var it in Buttons) {
      BroadcastButtonEvent(it.Key.Item1, it.Key.Item2, it.Value);
    }
  }
}