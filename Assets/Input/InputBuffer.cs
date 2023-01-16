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

General questions about integration:

  How do we handle choosing which input to respect when multiple inputs have happened within
  some threshold?

  For example, if jump was pushed on frame 2 and attack was pushed on frame 3 do we prioritize
  attack over jump?

  Let's say we attempt to process jump on frame 2, and on frame 3. It is pushed on frame 2 but
  you were still in the air. On frame 3, we are on the ground and attack and jump are both
  considered to have been pushed and thus both fire. What happens?

  How are we handling concurrent input currently in general?

  I believe if you press two buttons simultaneously today, they will happen to be processed in
  the following order:

    l1
    l2
    r1
    r2
    n
    e
    s
    w

  This ordering is arbitrary and comes from our InputManager code.

Comments on best practices:

  Normally, you should analyze your inputs and decide which you will consume on a given frame
  in some kind of input-processing script. For example, if a character is in the previous
  scenario where they pushed jump within a buffer (unconsumed) and they push attack on a frame
  it would look something like this:

  var jumpFrame = Buffer.Jump.Pressed
  var attackFrame = Buffer.Attack.Pressed

  if (attackFrame.HasValue && jumpFrame.HasValue) {
    if (attackFrame.Value <= jumpFrame.Value) {
      Consume(Buffer.Attack.Pressed)
      Attack()
    } else if (CanJump) {
      Consume(Buffer.Jump.Pressed)
      Jump()
    }
  }
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

  public void ConsumePress(ButtonCode code) {
    UpdateOrAdd(Presses, code, 0);
  }

  public void ConsumeHold(ButtonCode code) {
    UpdateOrAdd(Holds, code, 0);
  }

  public void ConsumeRelease(ButtonCode code) {
    UpdateOrAdd(Releases, code, 0);
  }

  void UpdateOrAdd<K,V>(Dictionary<K,V> d, K k, V v) {
    d[k] = v;
    // if (d.ContainsKey(k)) {
    //   d[k] = v;
    // } else {
    //   d.Add(k, v);
    // }
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
      ConsumePress(ButtonCode.L1);
      Debug.Log("Recent l1 push");
    }
  }
}