using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Inputs {
  public static Action Action;
  public static bool InPlayBack;
}

[Serializable]
public struct ButtonState {
  public bool Down;
  public bool JustDown;
  public bool JustUp;
  public ButtonState(bool down, bool justDown, bool justUp) {
    Down = down;
    JustDown = justDown;
    JustUp = justUp;
  }
  public void UpdateFromKeycode(KeyCode code) {
    Down = Down || Input.GetKey(code);
    JustDown = JustDown || Input.GetKeyDown(code);
    JustUp = JustUp || Input.GetKeyUp(code);
  }
  public void UpdateFromMouse(int button) {
    Down = Down || Input.GetMouseButton(button);
    JustDown = JustDown || Input.GetMouseButtonDown(button);
    JustUp = JustUp || Input.GetMouseButtonUp(button);
  }
  public void UpdateFromInput(string name) {
    Down = Down || Input.GetButton(name);
    JustDown = JustDown || Input.GetButtonDown(name);
    JustUp = JustUp || Input.GetButtonUp(name);
  }
  public void Reset() {
    Down = false;
    JustDown = false;
    JustUp = false;
  }
  public static ButtonState FromInput(string name) {
    return new ButtonState(Input.GetButton(name), Input.GetButtonDown(name), Input.GetButtonUp(name));
  }
}

[Serializable]
public struct StickState {
  public Vector2 RawXY;
  public Vector2 XY;
  public Vector3 RawXZ;
  public Vector3 XZ;
  public StickState(float deadZone, Vector2 raw) {
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
  public static StickState FromInput(float deadZone, string xname, string yname) {
    return new StickState(deadZone, new Vector2(Input.GetAxisRaw(xname), Input.GetAxisRaw(yname)));
  }
}

[Serializable]
public readonly struct Action {
  public readonly ButtonState North;
  public readonly ButtonState East;
  public readonly ButtonState South;
  public readonly ButtonState West;
  public readonly ButtonState L1;
  public readonly ButtonState L2;
  public readonly ButtonState R1;
  public readonly ButtonState R2;
  public readonly StickState Left;
  public readonly StickState Right;
  public Action(
    ButtonState north,
    ButtonState east,
    ButtonState south,
    ButtonState west,
    ButtonState l1,
    ButtonState l2,
    ButtonState r1,
    ButtonState r2,
    StickState left,
    StickState right) {
      North = north;
      East = east;
      South = south;
      West = west;
      L1 = l1;
      L2 = l2;
      R1 = r1;
      R2 = r2;
      Left = left;
      Right = right;
  }
}

[Serializable]
public enum PlayState { Idle, Play, PlayBack }

public class EventDriver : MonoBehaviour {
  [Header("Configuration")]
  public float RadialDeadZone;
  public bool UseMouseAndKeyboard = false;

  [Header("State")]
  public PlayState PlayState;

  Camera Camera;
  Player Player;

  List<Action> History = new List<Action>((int)Math.Pow(2, 16));
  int HistoryIndex = 0;

  ButtonState North;
  ButtonState East;
  ButtonState South;
  ButtonState West;
  ButtonState L1;
  ButtonState L2;
  ButtonState R1;
  ButtonState R2;
  StickState Left;
  StickState Right;

  void Start() {
    var objs = FindObjectsOfType<EventDriver>();
    if (objs.Length > 1) {
      Destroy(gameObject);
      return;
    }
    transform.SetParent(null, true);
    DontDestroyOnLoad(this.gameObject);

    Timeval.FrameCount = 0;
    Time.fixedDeltaTime = 1f / Timeval.FramesPerSecond;
    PlayState = PlayState.Play;
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
    Player = FindObjectOfType<Player>();
    Camera = FindObjectOfType<Camera>();
  }

  void OnDestroy() {
    History.Clear();
  }

  public void RestartScene() {
    Timeval.FrameCount = 0;
    Time.timeScale = 1;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //SceneManager.MoveGameObjectToScene(this.gameObject, scene);
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
  }

  public void PlaybackScene() {
    Timeval.FrameCount = 0;
    Time.timeScale = 1;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    HistoryIndex = 0;
    Inputs.InPlayBack = true;
    PlayState = PlayState.PlayBack;
  }

  Vector2 GetKeyboardMove() {
    float right = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
    float up = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
    return new Vector2(right, up);
  }

  Vector2 GetMouseAim() {
    var playerPos = Camera.WorldToScreenPoint(Player.transform.position);
    var playerPos2 = new Vector2(playerPos.x, playerPos.y);
    var mousePos2 = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    return (mousePos2 - playerPos2).normalized;
  }

  // Cache/accumulate input changes in Update. Feed them into simulation on FixedUpdate.
  void Update() {
    if (UseMouseAndKeyboard) {
      L1.UpdateFromKeycode(KeyCode.LeftShift);
      R1.UpdateFromMouse(0);
      R2.UpdateFromMouse(1);
      Left = new StickState(0f, GetKeyboardMove());
      Right = new StickState(0f, GetMouseAim());
    } else {
      North.UpdateFromInput("North");
      East.UpdateFromInput("East");
      South.UpdateFromInput("South");
      West.UpdateFromInput("West");
      L1.UpdateFromInput("L1");
      L2.UpdateFromInput("L2");
      R1.UpdateFromInput("R1");
      R2.UpdateFromInput("R2");
      Left = StickState.FromInput(RadialDeadZone, "LeftX", "LeftY");
      Right = StickState.FromInput(RadialDeadZone, "RightX", "RightY");
    }
  }

  void FixedUpdate() {
    Timeval.FrameCount++;
    switch (PlayState) {
    case PlayState.Play: {
        // Record the current values the down status of buttons right before consumption
        if (UseMouseAndKeyboard) {
          R1.Down = Input.GetMouseButton(0);
          R2.Down = Input.GetMouseButton(1);
        } else {
          North.Down = Input.GetButton("North");
          East.Down = Input.GetButton("East");
          South.Down = Input.GetButton("South");
          West.Down = Input.GetButton("West");
          L1.Down = Input.GetButton("L1");
          L2.Down = Input.GetButton("L2");
          R1.Down = Input.GetButton("R1");
          R2.Down = Input.GetButton("R2");
        }
        var action = new Action(North, East, South, West, L1, L2, R1, R2, Left, Right);
        Inputs.InPlayBack = false;
        Inputs.Action = action;
        History.Add(action);
        North.Reset();
        East.Reset();
        South.Reset();
        West.Reset();
        L1.Reset();
        L2.Reset();
        R1.Reset();
        R2.Reset();
      }
      break;

    case PlayState.PlayBack: {
        Inputs.InPlayBack = true;
        if (HistoryIndex < History.Count) {
          Inputs.Action = History[HistoryIndex++];
        } else {
          Inputs.Action = new Action();
        }
      }
      break;
    }
  }
}