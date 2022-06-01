using System;
using System.Collections.Generic;
using UnityEngine;

/*
GAME TIME - 1
INPUT TIME - GAME TIME / DILATION

PLAY CONTROLLER
  Each timestep of real-time, capture the current inputs.
  Drive the simulation with INPUT TIME.
  Record the inputs in GAME TIME.

REPLAY CONTROLLER
  Each timestep of real-time, find inputs that have occured since last step.
  Drive the simulation with the inputs found.
*/
[Serializable]
public struct Action {
  public float dt;
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

  [Header("Play")]
  public float GameTime;
  public float DilatedTime;
  public float Dilation;

  [Header("PlayBack")]
  public List<Action> History;
  public int HistoryIndex;
  public float ElapsedPlaybackTime;
  public float ElapsedPlaybackSimulationTime;

  [Header("Simulation")]
  public GameObject Avatar;

  void RunSimulation(Action action) {
    var dt = action.dt;
    var position = Avatar.transform.position + dt * new Vector3(action.Move.x,0,action.Move.y);
    var aim = action.Aim.magnitude > 0 ? new Vector3(action.Aim.x,0,action.Aim.y) : Avatar.transform.forward;
    Avatar.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim,Vector3.up));
  }

  [ContextMenu("Play")]
  void Start() {
    Avatar.transform.SetPositionAndRotation(Vector3.zero,Quaternion.LookRotation(Vector3.forward,Vector3.up));
    GameTime = 1;
    DilatedTime = 1;
    Dilation = 1;
    History.Clear();
    HistoryIndex = 0;
    PlayState = PlayState.Play;
  }

  [ContextMenu("Play Back")]
  void PlayBack() {
    Avatar.transform.SetPositionAndRotation(Vector3.zero,Quaternion.LookRotation(Vector3.forward,Vector3.up));
    GameTime = 1;
    DilatedTime = 1;
    Dilation = 1;
    HistoryIndex = 0;
    ElapsedPlaybackTime = 0;
    ElapsedPlaybackSimulationTime = 0;
    PlayState = PlayState.PlayBack;
  }

  void Update() {
    var dtReal = Time.deltaTime;
    switch (PlayState) {
      case PlayState.Play: {
        var move = new Vector2(Input.GetAxisRaw("MoveX"),Input.GetAxisRaw("MoveY"));
        var aim = new Vector2(Input.GetAxisRaw("AimX"),Input.GetAxisRaw("AimY"));
        var action = new Action {
          dt = dtReal,
          Hit = Input.GetButton("Action1"),
          Pounce = Input.GetButton("Action2"),
          Move = move.magnitude > RadialDeadZone ? move : Vector2.zero,
          Aim = aim.magnitude > RadialDeadZone ? aim : Vector2.zero,
        };
        History.Add(action);
        RunSimulation(action);
      }
      break;

      case PlayState.PlayBack: {
        ElapsedPlaybackTime += dtReal;
        var actions = new List<Action>();
        for (int i = HistoryIndex; i < History.Count; i++) {
          var action = History[i];
          var elapsed = ElapsedPlaybackSimulationTime + action.dt;
          if (elapsed <= ElapsedPlaybackTime) {
            HistoryIndex = i+1;
            ElapsedPlaybackSimulationTime = elapsed;
            RunSimulation(action);
          } else {
            break;
          }
          ElapsedPlaybackSimulationTime += action.dt;
        }
      }
      break;
    }
  }
}