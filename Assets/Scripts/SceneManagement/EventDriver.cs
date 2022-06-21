using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
  public Vector3 MoveXZ { get => new Vector3(Move.x,0,Move.y); }
  public Vector3 AimXZ { get => new Vector3(Aim.x,0,Aim.y); }
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

  Camera Camera;
  Player Player;

  List<Action> History = new List<Action>((int)Math.Pow(2,16));
  int HistoryIndex = 0;
  bool HitDown;
  bool HitUp;
  bool PounceDown;
  bool PounceUp;

  void Start() {
    var objs = FindObjectsOfType<EventDriver>();
    if (objs.Length > 1) {
      Destroy(gameObject);
      return;
    }
    DontDestroyOnLoad(this.gameObject);

    const float BASE_FIXED_DELTA_TIME = 0.02f;
    Time.fixedDeltaTime = BASE_FIXED_DELTA_TIME * .1f;
    if (!Room && RoomPrefab) {
      Room = Instantiate(RoomPrefab);
    }
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

  [ContextMenu("Play")]
  public void Play() {
    if (Room) {
      Destroy(Room.gameObject);
    }
    Room = Instantiate(RoomPrefab);
    History.Clear();
    HistoryIndex = 0;
    Inputs.InPlayBack = false;
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
    PounceDown = PounceDown || Input.GetButtonDown("Action2") || Input.GetKeyDown(KeyCode.Space);
    PounceUp = PounceUp || Input.GetButtonUp("Action2") || Input.GetKeyUp(KeyCode.Space);
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
    return Input.GetMouseButton(1) ? (mousePos2 - playerPos2).normalized : new Vector2(0,0);
  }

  void FixedUpdate() {
    switch (PlayState) {
      case PlayState.Play: {
        var hit = Input.GetButton("Action1");
        var pounce = Input.GetButton("Action2") || Input.GetKey(KeyCode.Space);
        var move = new Vector2(Input.GetAxisRaw("MoveX"),Input.GetAxisRaw("MoveY")) + GetKeyboardMove();
        var aim = new Vector2(Input.GetAxisRaw("AimX"),Input.GetAxisRaw("AimY")) + GetMouseAim();
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