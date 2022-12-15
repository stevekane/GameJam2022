using System;
using UnityEngine;

public class RepeatDive : MonoBehaviour {
  public Jump Jump;
  public Dive Dive;
  public Timeval Wait = Timeval.FromSeconds(1);

  TaskScope Scope = new();

  async void Start() {
    try {
      while (true) {
        await Scope.Run(Jump.MainAction);
        await Scope.Run(Dive.MainAction);
        await Scope.Delay(Wait);
      }
    } catch (Exception e) {
      Debug.Log("Stopped diving");
    }
  }

  void OnDestroy() {
    Scope.Cancel();
    Scope.Dispose();
  }
}