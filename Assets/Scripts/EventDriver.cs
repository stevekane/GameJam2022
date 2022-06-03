using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Action {
  public bool Hit;
  public bool Pounce;
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

  [Header("Screen")]
  public Screen ScreenPrefab;
  public Screen Screen;

  List<Action> History = new List<Action>((int)Math.Pow(2,16));
  int HistoryIndex = 0;

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
    if (Screen) {
      Destroy(Screen.gameObject);
    }
    Screen = Instantiate(ScreenPrefab);
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
    PlayState = PlayState.Play;
  }

  [ContextMenu("Play Back")]
  public void PlayBack() {
    Time.timeScale = 1;
    if (Screen) {
      Destroy(Screen.gameObject);
    }
    HistoryIndex = 0;
    Screen = Instantiate(ScreenPrefab);
    Inputs.InPlayBack = true;
    PlayState = PlayState.PlayBack;
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
          Pounce = pounce,
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
  }
}