using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/*
Input buffer is an implementation of retaining knowledge of a window of recent past
inputs and using that buffer to determine what inputs to process duing a frame.

Example use cases:

  Player intends to jump as soon as they land on a platform. They jump 2 frames before they
  actually land on the platform. Without some consideration of recent inputs, their input
  is swallowed leading to no jump and probably death.

  Player intends to execute a combo by following up light attack with a finisher. They press
  the finisher button 1 frame too early. Game swallows their input and they instead do nothing.

  Player intends to jump right before leaving a platform. They jump just after leaving the
  platform and instead fall to their death.

Design choices:

  Querying this system should allow you to ask "did this happen within the last X frames"
  to adjust buffers for various situations.

  Priority COULD be given to newer inputs to prevent execution of "stale" inputs.

  Buffer can cause multiple, unwanted inputs to be executed. Example is two jumps in the buffer
  could lead to immediate double-jumping. Possible solution is clearing the buffer once an
  input has been processed.
*/
public class InputBuffer : MonoBehaviour {
  [SerializeField] PlayerInputActions Controls;
  [SerializeField] int PlayerIndex;
  [SerializeField] int TickBufferLength = 10;

  Dictionary<ButtonCode, int> Presses = new();
  Dictionary<ButtonCode, int> Releases = new();
  Dictionary<ButtonCode, int> Holds = new();

  public int? Pressed(ButtonCode code, int frameDelta) {
    if (Presses.TryGetValue(code, out int tickCount)) {
      var delta = Timeval.TickCount - tickCount;
      return delta <= frameDelta ? delta : null;
    } else {
      return null;
    }
  }

  void UpdateOrAdd<K,V>(Dictionary<K,V> d, K k, V v) {
    if (d.ContainsKey(k)) {
      d[k] = v;
    } else {
      d.Add(k, v);
    }
  }

  void Record(ButtonCode code, InputAction action) {
    if (action.WasPressedThisFrame()) {
      UpdateOrAdd(Presses, code, Timeval.TickCount);
    }
    if (action.WasReleasedThisFrame()) {
      UpdateOrAdd(Releases, code, Timeval.TickCount);
    }
    if (action.IsPressed()) {
      UpdateOrAdd(Holds, code, Timeval.TickCount);
    }
  }

  void Awake() {
    Controls = new();
    if (PlayerIndex == 0) {
      Controls.devices = new InputDevice[] {
        Gamepad.all.First(g => g.name.Contains("DualShock"))
      };
    } else if (PlayerIndex == 1) {
      Controls.devices = new InputDevice[] {
        Gamepad.all.Last(g => g.name.Contains("DualShock"))
      };
    }
  }

  void OnEnable() => Controls.Enable();
  void OnDisable() => Controls.Disable();

  void FixedUpdate() {
    Record(ButtonCode.L1, Controls.Player.L1);
    Record(ButtonCode.L2, Controls.Player.L2);
    Record(ButtonCode.R1, Controls.Player.R1);
    Record(ButtonCode.R2, Controls.Player.R2);
    Record(ButtonCode.North, Controls.Player.North);
    Record(ButtonCode.East, Controls.Player.East);
    Record(ButtonCode.South, Controls.Player.South);
    Record(ButtonCode.West, Controls.Player.West);
    if (Pressed(ButtonCode.L1, TickBufferLength).HasValue) {
      Debug.Log("Recent l1 push");
    }
  }
}