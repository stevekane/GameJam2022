using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RootMotionMover : MonoBehaviour {
  [SerializeField] Transform Owner;
  [SerializeField] Animator Animator;

  void OnAnimatorMove() {
    Owner.position += Animator.deltaPosition;
    Owner.rotation = Animator.deltaRotation * Owner.rotation;
  }
}