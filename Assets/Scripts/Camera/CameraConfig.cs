using UnityEngine;

[CreateAssetMenu(fileName = "Camera", menuName = "Camera")]
public class CameraConfig : ScriptableObject {
  [Tooltip("Rate of time dilation decay")]
  [Range(-10, 0)]
  public float TIME_DELATION_DECAY_EPSILON = -0.5f;

  [Tooltip("Rate of interpolation for camera lookahead")]
  [Range(-10, 0)]
  public float LOOK_AHEAD_EPSILON = -0.5f;

  [Tooltip("Rate of interpolation for camera shake decay")]
  [Range(-10, 0)]
  public float SHAKE_DECAY_EPSILON = -0.5f;

  [Tooltip("Max intensity of camera shake")]
  [Range(0, 100)]
  public float MAX_SHAKE_INTENSITY = 5;
}