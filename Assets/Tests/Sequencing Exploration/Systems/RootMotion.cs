using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class RootMotion : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;

  public Vector3 DeltaPosition;
  public Quaternion DeltaRotation;

  void Awake() {
    enabled = false;
  }

  void OnAnimatorMove() {
    DeltaPosition = Animator.deltaPosition;
    DeltaRotation = Animator.deltaRotation;
  }

  void FixedUpdate() {
    Controller.Move(DeltaPosition);
  }
}