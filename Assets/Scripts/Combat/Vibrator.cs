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

  public void VibrateOnHit(Vector3 axis, int frames) => Vibrate(axis, frames, HitAmplitude);
  public void VibrateOnHurt(Vector3 axis, int frames) => Vibrate(axis, frames, HurtAmplitude);

  void Start() {
    Target = GetComponent<AvatarAttacher>()?.GetBoneTransform(AvatarBone.Hips)?.parent ?? Target;
    LocalPosition = Target.transform.localPosition;
  }

  void FixedUpdate() {
    if (FramesRemaining > 0) {
      Sign *= -1;
      Target.transform.localPosition = LocalPosition+Sign*Amplitude*Axis;
      FramesRemaining--;
    } else {
      Target.transform.localPosition = LocalPosition;
    }
  }
}