using UnityEngine;

public delegate void IKCallback();
public delegate void RootMotionCallback(Vector3 v);
public delegate void RootRotationCallback(Quaternion q);

[RequireComponent(typeof(Animator))]
public class RootMotionListener : MonoBehaviour {
  public Animator Animator;
  public RootMotionCallback OnRootMotion;
  public RootRotationCallback OnRootRotation;
  public IKCallback IKCallback;
  void OnAnimatorMove() {
    OnRootMotion?.Invoke(Animator.deltaPosition);
    OnRootRotation?.Invoke(Animator.deltaRotation);
  }
  void OnAnimatorIK() {
    IKCallback?.Invoke();
  }
}