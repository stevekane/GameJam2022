using System;
using Cinemachine;
using UnityEngine;

public class CameraShaker : CinemachineExtension {
  [SerializeField] CameraConfig Config;
  CinemachineVirtualCamera TargetCamera;
  CinemachineBasicMultiChannelPerlin Noise;

  public static CameraShaker Instance;

  public void Shake(float targetIntensity) {
    Noise.m_AmplitudeGain = Mathf.Min(Noise.m_AmplitudeGain+targetIntensity, Config.MAX_SHAKE_INTENSITY);
  }

  protected override void Awake() {
    base.Awake();
    Instance = this;
  }

  protected override void ConnectToVcam(bool connect) {
    base.ConnectToVcam(connect);
    TargetCamera = VirtualCamera as CinemachineVirtualCamera;
    Noise = TargetCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
  }

  protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float dt) {
    Noise.m_AmplitudeGain = Mathf.Lerp(0, Noise.m_AmplitudeGain, Mathf.Exp(dt*Config.SHAKE_DECAY_EPSILON));
  }
}