using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
  public readonly ButtonState Hit;
  public readonly ButtonState Jump;
  public readonly ButtonState Light;
  public readonly ButtonState Heavy;
  public readonly ButtonState Dash;
  public readonly ButtonState Throw;
  public readonly StickState Move;
  public readonly StickState Aim;
  public Action(
    ButtonState hitState,
    ButtonState jumpState,
    ButtonState lightState,
    ButtonState heavyState,
    ButtonState dashState,
    ButtonState throwState,
    StickState move,
    StickState aim) {
    Hit = hitState;
    Jump = jumpState;
    Light = lightState;
    Heavy = heavyState;
    Dash = dashState;
    Throw = throwState;
    Move = move;
    Aim = aim;
  }
}

[Serializable]
public enum PlayState { Idle, Play, PlayBack }

public class EventDriver : MonoBehaviour {
  [Header("Configuration")]
  public float RadialDeadZone;

  [Header("State")]
  public PlayState PlayState;

  Camera Camera;
  Player Player;

  List<Action> History = new List<Action>((int)Math.Pow(2, 16));
  int HistoryIndex = 0;

  ButtonState Hit;
  ButtonState Jump;
  ButtonState Light;
  ButtonState Heavy;
  ButtonState Dash;
  ButtonState Throw;
  StickState Move;
  StickState Aim;

  void Start() {
    var objs = FindObjectsOfType<EventDriver>();
    if (objs.Length > 1) {
      Destroy(gameObject);
      return;
    }
    DontDestroyOnLoad(this.gameObject);

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
    Time.timeScale = 1;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    //SceneManager.MoveGameObjectToScene(this.gameObject, scene);
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
  }

  public void PlaybackScene() {
    Time.timeScale = 1;
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    HistoryIndex = 0;
    Inputs.InPlayBack = true;
    PlayState = PlayState.PlayBack;
  }

  // Cache/accumulate input changes in Update. Feed them into simulation on FixedUpdate.
  void Update() {
    Hit.UpdateFromInput("Hit");
    Jump.UpdateFromInput("Jump");
    Light.UpdateFromInput("Light");
    Heavy.UpdateFromInput("Heavy");
    Dash.UpdateFromInput("Dash");
    Throw.UpdateFromInput("Throw");
    Move = StickState.FromInput(RadialDeadZone, "MoveX", "MoveY");
    Aim = StickState.FromInput(RadialDeadZone, "AimX", "AimY");
  }

  void FixedUpdate() {
    switch (PlayState) {
    case PlayState.Play: {
        // Record the current values the down status of buttons right before consumption
        Hit.Down = Input.GetButton("Hit");
        Jump.Down = Input.GetButton("Jump");
        Light.Down = Input.GetButton("Light");
        Heavy.Down = Input.GetButton("Heavy");
        Dash.Down = Input.GetButton("Dash");
        Throw.Down = Input.GetButton("Throw");
        var action = new Action(Hit, Jump, Light, Heavy, Dash, Throw, Move, Aim);
        Inputs.InPlayBack = false;
        Inputs.Action = action;
        History.Add(action);
        Hit.Reset();
        Jump.Reset();
        Light.Reset();
        Heavy.Reset();
        Dash.Reset();
        Throw.Reset();
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