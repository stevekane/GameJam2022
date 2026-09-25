using System;

using UnityEngine;

public class CameraShaker : Unity.Cinemachine.CinemachineExtension {
  [SerializeField] CameraConfig Config;
  Unity.Cinemachine.CinemachineVirtualCamera TargetCamera;
  Unity.Cinemachine.CinemachineBasicMultiChannelPerlin Noise;

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
    TargetCamera = VirtualCamera as Unity.Cinemachine.CinemachineVirtualCamera;
    Noise = TargetCamera.GetCinemachineComponent<Unity.Cinemachine.CinemachineBasicMultiChannelPerlin>();
  }

  protected override void PostPipelineStageCallback(Unity.Cinemachine.CinemachineVirtualCameraBase vcam, Unity.Cinemachine.CinemachineCore.Stage stage, ref Unity.Cinemachine.CameraState state, float dt) {
    Noise.m_AmplitudeGain = Mathf.Lerp(0, Noise.m_AmplitudeGain, Mathf.Exp(dt*Config.SHAKE_DECAY_EPSILON));
  }
}