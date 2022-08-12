using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[Serializable]
public abstract class Ability : MonoBehaviour {
  protected Bundle Bundle = new();
  public AbilityManager AbilityManager { get; set; }
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public bool IsRunning { get => Bundle.IsRunning; }
  public bool IsRoutineRunning(Fiber f) => Bundle.IsRoutineRunning(f);
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  public virtual void Stop() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}