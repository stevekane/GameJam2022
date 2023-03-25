using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late+1)]
public class RootMotion : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;

  public Vector3 DeltaPosition;
  public Quaternion DeltaRotation;

  void Awake() {
    enabled = false;
  }

  // N.B. Fires during Animation Execution Group
  void OnAnimatorMove() {
    DeltaPosition = Animator.deltaPosition;
    DeltaRotation = Animator.deltaRotation;
  }

  // N.B. Fires during Late Execution Group
  void FixedUpdate() {
    var rotation = DeltaRotation * transform.rotation;
    var euler = rotation.eulerAngles;
    rotation = Quaternion.Euler(0, euler.y, 0);
    transform.rotation = rotation;
    Controller.Move(DeltaPosition);
  }
}