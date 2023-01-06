using UnityEngine;

namespace Traditional {
  public class LaunchSelf : MonoBehaviour {
    [SerializeField] CharacterController CharacterController;
    [SerializeField] AudioSource AudioSource;
    [SerializeField] Animator Animator;
    [SerializeField] FallSpeed FallSpeed;
    [SerializeField] Gravity Gravity;
    [SerializeField] MoveSpeed MoveSpeed;
    [SerializeField] TurnSpeed TurnSpeed;
    [SerializeField] AudioClip TakeoffSFX;
    [SerializeField] GameObject TakeoffVFX;
    [SerializeField] AudioClip LandingSFX;
    [SerializeField] GameObject LandingVFX;
    [SerializeField] float LaunchHeight = 10;
    [SerializeField] Timeval TumbleDuration = Timeval.FromSeconds(1);
    int Remaining = 0;

    void OnEnable() {
      // mvv/2 = mgh from conservation of energy. solve for v: v = (2gh)^(1/2)
      FallSpeed.Add(Mathf.Sqrt(2 * -Gravity.Value * LaunchHeight));
      Remaining = TumbleDuration.Ticks;
      AudioSource.PlayOptionalOneShot(TakeoffSFX);
      var rotation = Quaternion.LookRotation(Vector3.up, transform.forward.XZ());
      Destroy(Instantiate(TakeoffVFX, transform.position, rotation), 2);
    }

    void OnDisable() {
      Animator.SetBool("IsHurt", false);
    }

    void OnLand() {
      if (enabled) {
        enabled = false;
        Remaining = 0;
        AudioSource.PlayOptionalOneShot(LandingSFX);
        var rotation = Quaternion.LookRotation(transform.forward.XZ(), Vector3.up);
        Destroy(Instantiate(LandingVFX, transform.position, rotation), 2);
      }
    }

    void FixedUpdate() {
      var active = Remaining > 0;
      Animator.SetBool("IsHurt", active);
      MoveSpeed.Mul(0);
      TurnSpeed.Mul(0);
      enabled = active;
      Remaining--;
    }
  }
}