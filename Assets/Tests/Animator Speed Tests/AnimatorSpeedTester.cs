using UnityEngine;

public class AnimatorSpeedTester : MonoBehaviour {
  public Animator Animator;
  public int FixedUpdateCount;
  public int FixedFreezeFrame;

  void Start() {
    FixedUpdateCount = 0;
  }

  void FixedUpdate() {
    Animator.speed = FixedUpdateCount == FixedFreezeFrame ? 0 : Animator.speed;
    FixedUpdateCount++;
  }
}