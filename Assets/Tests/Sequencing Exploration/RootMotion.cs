using UnityEngine;

public class RootMotion : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;

  void Awake() {
    enabled = false;
  }

  void OnAnimatorMove() {
    Controller.Move(Animator.deltaPosition);
  }
}