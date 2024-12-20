using System;
using UnityEngine;

public class AnimationDriverTester : MonoBehaviour {
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] AnimationJobConfig AnimationConfig;
  [SerializeField] Timeval Period = Timeval.FromSeconds(1);

  TaskScope scope = new();
  async void Start() {
    try {
      await scope.Repeat(async s => {
        try {
          AnimationDriver.Play(s, AnimationConfig);
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