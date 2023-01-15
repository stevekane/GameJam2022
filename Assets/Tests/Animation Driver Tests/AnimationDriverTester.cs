using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationDriverTester : MonoBehaviour {
  public Animator Animator;
  public AnimationDriver AnimationDriver;
  public AnimationJobConfig AnimationConfig;

  TaskScope scope = new();
  async void Start() {
    try {
      await scope.Repeat(async s => {
        try {
          Animator.SetTrigger("Attack");
          AnimationDriver.Play(s, AnimationConfig);
          await s.Delay(Timeval.FromSeconds(3));
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

  #if UNITY_EDITOR
  void OnDrawGizmos() {
    Handles.Label(Animator.transform.position + 2 * Vector3.up, "Animator");
    Handles.Label(AnimationDriver.transform.position + 2 * Vector3.up, "Animation Driver");
  }
  #endif
}