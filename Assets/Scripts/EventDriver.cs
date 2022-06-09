using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Action {
  public bool Hit;
  public bool HitDown;
  public bool HitUp;
  public bool Pounce;
  public bool PounceDown;
  public bool PounceUp;
  public Vector2 Move;
  public Vector2 Aim;
}

[Serializable]
public enum PlayState { Idle, Play, PlayBack }

public class EventDriver : MonoBehaviour {
  [Header("Configuration")]
  public float RadialDeadZone;

  [Header("State")]
  public PlayState PlayState;

  [Header("Room")]
  public Room RoomPrefab;
  public Room Room;

  List<Action> History = new List<Action>((int)Math.Pow(2,16));
  int HistoryIndex = 0;
  bool HitDown;
  bool HitUp;
  bool PounceDown;
  bool PounceUp;

  void Start() {
    const float BASE_FIXED_DELTA_TIME = 0.02f;
    Time.fixedDeltaTime = BASE_FIXED_DELTA_TIME * .1f;
    Play();
  }

  void OnDestroy() {
    History.Clear();
  }

  [ContextMenu("Play")]
  public void Play() {
    if (Room) {
      Destroy(Room.gameObject);
    }
    Room = Instantiate(RoomPrefab);
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
    PlayState = PlayState.Play;
  }

  [ContextMenu("Play Back")]
  public void PlayBack() {
    Time.timeScale = 1;
    if (Room) {
      Destroy(Room.gameObject);
    }
    HistoryIndex = 0;
    Room = Instantiate(RoomPrefab);
    Inputs.InPlayBack = true;
    PlayState = PlayState.PlayBack;
  }

  /*
  Input is changed in Unity's Update loop.
  We want to consume it during Fixed Updates.

  In Update we should just do the following:

    Down = Down || Input.Down
    Up = Up || Input.Up
  
  In FixedUpdate we use these states before resetting them.
    Simulate(Up,Down)
    Down = false
    Up = false
  */
  void Update() {
    HitDown = HitDown || Input.GetButtonDown("Action1");
    HitUp = HitUp || Input.GetButtonUp("Action1");
    PounceDown = PounceDown || Input.GetButtonDown("Action2");
    PounceUp = PounceUp || Input.GetButtonUp("Action2");
  }

  void FixedUpdate() {
    switch (PlayState) {
      case PlayState.Play: {
        var hit = Input.GetButton("Action1");
        var pounce = Input.GetButton("Action2");
        var move = new Vector2(Input.GetAxisRaw("MoveX"),Input.GetAxisRaw("MoveY"));
        var aim = new Vector2(Input.GetAxisRaw("AimX"),Input.GetAxisRaw("AimY"));
        move = move.magnitude > RadialDeadZone ? move : Vector2.zero;
        aim = aim.magnitude > RadialDeadZone ? aim : Vector2.zero;
        var action = new Action {
          Hit = hit,
          HitDown = HitDown,
          HitUp = HitUp,
          Pounce = pounce,
          PounceDown = PounceDown,
          PounceUp = PounceUp,
          Move = move,
          Aim = aim,
        };
        Inputs.Action = action;
        Inputs.InPlayBack = false;
        History.Add(action);
      }
      break;

      case PlayState.PlayBack: {
        Inputs.InPlayBack = true;
        if (HistoryIndex < History.Count) {
          Inputs.Action = History[HistoryIndex++];
        }
      }
      break;
    }
    HitDown = false;
    HitUp = false;
    PounceDown = false;
    PounceUp = false;
  }
}