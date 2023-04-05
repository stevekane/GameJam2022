using UnityEngine;

public class SamplePressAndReleaseActionsBindings : MonoBehaviour {
  [SerializeField] SampleAction Press;
  [SerializeField] SampleAction Release;
  [SerializeField] ButtonCode ButtonCode;
  [SerializeField] bool ConsumePressOnPress = true;
  [SerializeField] bool ConsumePressOnRelease = true;
  [SerializeField] bool ConsumeReleaseOnPress = true;
  [SerializeField] bool ConsumeReleaseOnRelease = true;

  InputManager InputManager;

  void TryFirePress() {
    if (Press.Satisfied) {
      if (ConsumePressOnPress)
        InputManager.Consume(ButtonCode, ButtonPressType.JustDown);
      if (ConsumeReleaseOnPress)
        InputManager.Consume(ButtonCode, ButtonPressType.JustUp);
      Press.Fire();
    }
  }

  void TryFireRelease() {
    if (Release.Satisfied) {
      if (ConsumePressOnRelease)
        InputManager.Consume(ButtonCode, ButtonPressType.JustDown);
      if (ConsumeReleaseOnRelease)
        InputManager.Consume(ButtonCode, ButtonPressType.JustUp);
      Release.Fire();
    }
  }

  void Start() {
    InputManager = GetComponentInParent<InputManager>();
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Listen(TryFirePress);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Listen(TryFireRelease);
  }

  void OnDestroy() {
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustDown).Listen(TryFirePress);
    InputManager.ButtonEvent(ButtonCode, ButtonPressType.JustUp).Unlisten(TryFireRelease);
  }
}