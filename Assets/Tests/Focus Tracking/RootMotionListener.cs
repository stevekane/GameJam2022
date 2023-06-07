using UnityEngine;

public delegate void RootMotionCallback(Vector3 v);
public delegate void RootRotationCallback(Quaternion q);

[RequireComponent(typeof(Animator))]
public class RootMotionListener : MonoBehaviour {
  public Animator Animator;
  public RootMotionCallback OnRootMotion;
  public RootRotationCallback OnRootRotation;
  void OnAnimatorMove() {
    OnRootMotion?.Invoke(Animator.deltaPosition);
    OnRootRotation?.Invoke(Animator.deltaRotation);
  }
}