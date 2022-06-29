using Cinemachine;
using UnityEngine;

public class ScreenPositionDriver : CinemachineExtension {
  [SerializeField] 
  CameraConfig Config;
  CinemachineVirtualCamera TargetCamera;
  CinemachineFramingTransposer Transposer;

  protected override void ConnectToVcam(bool connect) {
    base.ConnectToVcam(connect);
    TargetCamera = VirtualCamera as CinemachineVirtualCamera;
    Transposer = TargetCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
  }

  protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float dt) {
    if (!TargetCamera || !TargetCamera.Follow || !Transposer)
      return;

    var forwardxz = TargetCamera.Follow.transform.forward.XZ();
    var y = Vector3.Dot(forwardxz,Vector3.forward);
    var x = Vector3.Dot(forwardxz,Vector3.right);
    var currentScreenX = Transposer.m_ScreenX;
    var currentScreenY = Transposer.m_ScreenY;
    var targetScreenX = Mathf.Lerp(1,0,Mathf.InverseLerp(-1,1,x));
    var targetScreenY = Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,y));
    var interpolant = Mathf.Exp(Config.LOOK_AHEAD_EPSILON*Time.fixedDeltaTime);
    Transposer.m_ScreenX = Mathf.Lerp(targetScreenX,currentScreenX,interpolant);
    Transposer.m_ScreenY = Mathf.Lerp(targetScreenY,currentScreenY,interpolant);
  }
}