using Cinemachine;
using UnityEngine;

[ExecuteInEditMode]
public class ScreenPositionDriver : MonoBehaviour {
  [SerializeField] 
  [Range(-100f,0f)]
  float ScreenInterpolationEpsilon = -.1f;
  CinemachineVirtualCamera TargetCamera;
  CinemachineFramingTransposer Transposer;

  void Awake() {
    TargetCamera = GetComponent<CinemachineVirtualCamera>();
    Transposer = TargetCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
  }

  void LateUpdate() {
    if (!TargetCamera || !TargetCamera.Follow || !Transposer)
      return;

    var forwardxz = TargetCamera.Follow.transform.forward.XZ();
    var y = Vector3.Dot(forwardxz,Vector3.forward);
    var x = Vector3.Dot(forwardxz,Vector3.right);
    var currentScreenX = Transposer.m_ScreenX;
    var currentScreenY = Transposer.m_ScreenY;
    var targetScreenX = Mathf.Lerp(1,0,Mathf.InverseLerp(-1,1,x));
    var targetScreenY = Mathf.Lerp(0,1,Mathf.InverseLerp(-1,1,y));
    var interpolant = Mathf.Exp(ScreenInterpolationEpsilon*Time.fixedDeltaTime);
    Transposer.m_ScreenX = Mathf.Lerp(targetScreenX,currentScreenX,interpolant);
    Transposer.m_ScreenY = Mathf.Lerp(targetScreenY,currentScreenY,interpolant);
  }
}