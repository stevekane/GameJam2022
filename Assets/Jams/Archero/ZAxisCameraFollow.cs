using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class ZAxisCameraFollow : CinemachineExtension {
  [SerializeField] float Speed = 1f;

  Vector3 InitialOffset;
  Vector3 TargetPosition;
  Transform CurrentTarget;

  public Transform Target {
    get => CurrentTarget;
    set {
      CurrentTarget = value;
      InitialOffset = transform.position - CurrentTarget.position;
    }
  }

  protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime) {
    if (Target && stage == CinemachineCore.Stage.Body) {
      TargetPosition = CurrentTarget.position + InitialOffset;
    }
  }

  private void LateUpdate() {
    if (Target) {
      TargetPosition.z = CurrentTarget.position.z + InitialOffset.z;
      TargetPosition.x = transform.position.x;
      TargetPosition.y = transform.position.y;
      transform.position = Vector3.Lerp(transform.position, TargetPosition, Speed * Time.deltaTime);
    }
  }
}
