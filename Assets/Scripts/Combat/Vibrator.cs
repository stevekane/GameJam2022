using UnityEngine;

public class Vibrator : MonoBehaviour {
  [SerializeField] Transform Target;
  [SerializeField] Timeval HitDuration = Timeval.FromMillis(200);
  [SerializeField] Timeval HurtDuration = Timeval.FromMillis(300);
  [SerializeField] float HitAmplitude = .1f;
  [SerializeField] float HurtAmplitude = .2f;

  Vector3 Axis;
  Vector3 LocalPosition;
  float Amplitude;
  int FramesRemaining;
  int Sign = 1;

  public void Vibrate(Vector3 axis, int frames, float amplitude) {
    Axis = axis;
    Amplitude = amplitude;
    FramesRemaining = Mathf.Max(FramesRemaining,frames);
  }

  public void VibrateThisFrame(Vector3 axis, float amplitude) {
    Axis = axis;
    Amplitude = amplitude;
    FramesRemaining++;
  }

  void Start() {
    LocalPosition = Target.transform.localPosition;
  }

  void OnHit(HitParams hitParams) {
    Vibrate(hitParams.KnockbackVector, HitDuration.Ticks, HitAmplitude);
  }

  void OnHurt(HitParams hitParams) {
    Vibrate(hitParams.KnockbackVector, HurtDuration.Ticks, HurtAmplitude);
  }

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      Sign = Sign == -1 ? 1 : -1;
      Target.transform.localPosition = LocalPosition+Sign*Amplitude*Axis;
      FramesRemaining--;
    } else {
      Target.transform.localPosition = LocalPosition;
    }
  }
}