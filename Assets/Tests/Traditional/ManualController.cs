using UnityEngine;

namespace Traditional {
  public class ManualController : MonoBehaviour {
    [SerializeField] InputManager InputManager;
    [SerializeField] MoveDirection MoveDirection;
    [SerializeField] AimDirection AimDirection;
    [SerializeField] LocalTimeScale LocalTimeScale;
    [SerializeField] AxisCode MoveAxisCode = AxisCode.AxisLeft;
    [SerializeField] AxisCode AimAxisCode = AxisCode.AxisRight;

    void Start() {
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustDown).Listen(SlowTime);
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustUp).Listen(ResumeTime);
    }

    void Stop() {
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustDown).Unlisten(SlowTime);
      InputManager.ButtonEvent(ButtonCode.North, ButtonPressType.JustUp).Unlisten(ResumeTime);
    }

    void SlowTime() => LocalTimeScale.Base = .5f;
    void ResumeTime() => LocalTimeScale.Base = 1;

    void FixedUpdate() {
      MoveDirection.Base = InputManager.Axis(MoveAxisCode).XZ;
      AimDirection.Base = InputManager.Axis(AimAxisCode).XZ;
    }
  }
}