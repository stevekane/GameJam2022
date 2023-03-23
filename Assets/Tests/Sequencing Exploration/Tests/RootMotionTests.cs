using UnityEngine;

public class RootMotionTests : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField, Range(0, 1)] float Offset;

  void Update() {
    Animator.PlayInFixedTime("Land", 0, Offset);
    if (Input.GetKeyDown(KeyCode.B)) {
      Animator.CrossFadeInFixedTime("Idle", .25f);
    } else if (Input.GetKeyDown(KeyCode.N)) {
      Animator.CrossFadeInFixedTime("Fall", .25f);
    } else if (Input.GetKeyDown(KeyCode.M)) {
      Animator.CrossFadeInFixedTime("Land", .1f);
    }
  }
}