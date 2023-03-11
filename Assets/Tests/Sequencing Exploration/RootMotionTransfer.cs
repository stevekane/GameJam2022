using UnityEngine;

public class RootMotionTransfer : MonoBehaviour {
  [SerializeField] Animator Animator;

  void Awake() {
    enabled = false;
  }

  void OnAnimatorMove() {
    var target = FindObjectOfType<TargetDummyController>();
    target.SendMessage("OnSynchronizedMove", Animator.deltaPosition);
  }
}