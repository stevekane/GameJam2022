using System;
using UnityEngine;

public class AnimatorTester : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] Timeval Period = Timeval.FromSeconds(1);

  TaskScope scope = new();
  async void Start() {
    try {
      await scope.Repeat(async s => {
        try {
          Animator.Play("Attack");
          await s.Delay(Period);
        } catch (Exception e) {
          Debug.LogWarning(e.Message);
        }
      });
    } catch (Exception e) {
      Debug.LogWarning(e.Message);
    }
  }

  void OnDestroy() {
    scope.Dispose();
  }
}