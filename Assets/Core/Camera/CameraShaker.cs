using System;

using UnityEngine;
using Unity.Cinemachine;

public class CameraShaker : CinemachineExtension {
  [SerializeField] CameraConfig Config;
  Unity.Cinemachine.CinemachineCamera TargetCamera;
  Unity.Cinemachine.CinemachineBasicMultiChannelPerlin Noise;

  public static CameraShaker Instance;

  public void Shake(float targetIntensity) {
    Noise.AmplitudeGain = Mathf.Min(Noise.AmplitudeGain+targetIntensity, Config.MAX_SHAKE_INTENSITY);
  }

  protected override void Awake() {
    base.Awake();
    Instance = this;
  }

  protected override void ConnectToVcam(bool connect) {
    base.ConnectToVcam(connect);
    TargetCamera = GetComponent<CinemachineCamera>();
    Noise = TargetCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

  protected override void PostPipelineStageCallback(Unity.Cinemachine.CinemachineVirtualCameraBase vcam, Unity.Cinemachine.CinemachineCore.Stage stage, ref Unity.Cinemachine.CameraState state, float dt) {
    Noise.AmplitudeGain = Mathf.Lerp(0, Noise.AmplitudeGain, Mathf.Exp(dt*Config.SHAKE_DECAY_EPSILON));
  }
}