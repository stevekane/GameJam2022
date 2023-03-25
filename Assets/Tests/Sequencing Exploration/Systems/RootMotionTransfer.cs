using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late+1)]
public class RootMotionTransfer : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] MeleeAttackTargeting MeleeAttackTargeting;
  [SerializeField] RootMotion RootMotion;

  void Awake() {
    enabled = false;
  }

  void FixedUpdate() {
    foreach (var target in MeleeAttackTargeting.Victims) {
      target.SendMessage("OnSynchronizedMove", RootMotion.DeltaPosition);
    }
  }
}