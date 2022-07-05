using UnityEngine;

public class Vibrator : MonoBehaviour {
  [SerializeField] 
  Transform Target;

  Vector3 Axis;
  Vector3 LocalPosition;
  float Amplitude;
  int TotalFrames;
  int FramesRemaining;
  public void Vibrate(Vector3 axis, int frames, float amplitude) {
    LocalPosition = FramesRemaining > 0 ? LocalPosition : Target.transform.localPosition;
    Axis = axis;
    Amplitude = amplitude;
    TotalFrames = frames;
    FramesRemaining = frames;
  }

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      var sign = FramesRemaining%2 == 0 ? 1 : -1;
      Target.transform.localPosition = LocalPosition+sign*Amplitude*Axis;
      FramesRemaining--;
    } else {
      Target.transform.localPosition = LocalPosition;
    }
  }
}