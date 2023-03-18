using UnityEngine;

public class RootMotionTransfer : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;

  void Awake() {
    enabled = false;
  }

  void OnAnimatorMove() {
    foreach (var target in MeleeAttackTargeting.Victims) {
      target.SendMessage("OnSynchronizedMove", Animator.deltaPosition);
    }
  }
}