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

  [Header("Simulation")]
  public GameObject Avatar;
  public float Speed = 1f;
  public GameObject Enemy;
  public float RotationSpeed = Mathf.PI;
  [Range(1,4)]
  public float Dilation;

  List<Action> History = new List<Action>((int)Math.Pow(2,16));
  int HistoryIndex = 0;
  float ElapsedPlaybackTime = 0;
  float ElapsedPlaybackSimulationTime = 0;

  void RunSimulation(Action action) {
    var dt = action.dt;
    var position = Avatar.transform.position + Speed * dt * new Vector3(action.Move.x,0,action.Move.y);
    var aim = action.Aim.magnitude > 0 ? new Vector3(action.Aim.x,0,action.Aim.y) : Avatar.transform.forward;
    Avatar.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim,Vector3.up));
    Enemy.transform.RotateAround(Enemy.transform.position,Vector3.up,dt * RotationSpeed);
  }

  [ContextMenu("Play")]
  void Start() {
    Avatar.transform.SetPositionAndRotation(Vector3.zero,Quaternion.LookRotation(Vector3.forward,Vector3.up));
    Dilation = 4;
    History.Clear();
    HistoryIndex = 0;
    PlayState = PlayState.Play;
  }

  [ContextMenu("Play Back")]
  void PlayBack() {
    Avatar.transform.SetPositionAndRotation(Vector3.zero,Quaternion.LookRotation(Vector3.forward,Vector3.up));
    Dilation = 1;
    HistoryIndex = 0;
    ElapsedPlaybackTime = 0;
    ElapsedPlaybackSimulationTime = 0;
    PlayState = PlayState.PlayBack;
  }

  void Update() {
    switch (PlayState) {
      case PlayState.Play: {
        var dtReal = Time.deltaTime;
        var dtDilated = dtReal / Dilation;
        var hit = Input.GetButton("Action1");
        var pounce = Input.GetButton("Action2");
        var move = new Vector2(Input.GetAxisRaw("MoveX"),Input.GetAxisRaw("MoveY"));
        var aim = new Vector2(Input.GetAxisRaw("AimX"),Input.GetAxisRaw("AimY"));
        move = move.magnitude > RadialDeadZone ? move : Vector2.zero;
        aim = aim.magnitude > RadialDeadZone ? aim : Vector2.zero;
        var action = new Action {
          dt = dtDilated,
          Hit = hit,
          Pounce = pounce,
          Move = move,
          Aim = aim,
        };
        RunSimulation(action);
        History.Add(action);
      }
      break;

      case PlayState.PlayBack: {
        var dtReal = Time.deltaTime;
        ElapsedPlaybackTime += dtReal;
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
        }
      }
      break;
    }
  }
}