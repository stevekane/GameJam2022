using UnityEngine;

namespace Traditional {
  public class ManualController : MonoBehaviour {
    [SerializeField] InputManager InputManager;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] AimDirection AimDirection;
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] Dash Dash;
    [SerializeField] LaunchSelf LaunchSelf;
    [SerializeField] AxisCode MoveAxisCode = AxisCode.AxisLeft;
    [SerializeField] AxisCode AimAxisCode = AxisCode.AxisRight;

    void Start() {
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustDown).Listen(SlowTime);
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustUp).Listen(ResumeTime);
      InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Listen(StartDash);
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Listen(StartLaunchSelf);
    }

    void Stop() {
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustDown).Unlisten(SlowTime);
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustUp).Unlisten(ResumeTime);
      InputManager.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Unlisten(StartDash);
      InputManager.ButtonEvent(ButtonCode.South, ButtonPressType.JustDown).Unlisten(StartLaunchSelf);
    }

    void SlowTime() => LocalTimeScale.Base = .5f;
    void ResumeTime() => LocalTimeScale.Base = 1;
    void StartDash() => Dash.enabled = true;
    void StartLaunchSelf() => LaunchSelf.enabled = true;

    void FixedUpdate() {
      MoveDirection.Base = InputManager.Axis(MoveAxisCode).XZ;
      AimDirection.Base = InputManager.Axis(AimAxisCode).XZ;
    }
  }
}