using UnityEngine;

public class AnimatorSpeedTester : MonoBehaviour {
  public Animator Animator;
  public int FixedUpdateCount;
  public int UpdateCount;
  public int FixedFreezeFrame;

  void Start() {
    FixedUpdateCount = 0;
  }

  void FixedUpdate() {
    if (FixedUpdateCount == FixedFreezeFrame) {
      Time.timeScale = 0;
      Animator.speed = 0;
    } else {
      FixedUpdateCount++;
    }
  }

  void Update() {
    UpdateCount++;
  }
}