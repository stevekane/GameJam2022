using System;
using UnityEngine;

public class RepeatAttack : MonoBehaviour {
  public Ability Attack;
  public Timeval Wait = Timeval.FromSeconds(1);

  TaskScope Scope = new();

  async void Start() {
    try {
      while (true) {
        await Scope.Delay(Wait);
        await Scope.Run(Attack.MainAction);
      }
    } catch (Exception) {
    }
  }

  void OnDestroy() {
    Scope.Cancel();
    Scope.Dispose();
  }
}