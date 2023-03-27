using UnityEngine;

public class GroundState : MonoBehaviour {
  [SerializeField] InputSystemTester SystemTester;
  [SerializeField] InputManager InputManager;
  [SerializeField] ButtonCode FlyButtonCode;

  void OnEnable() {
    InputManager.ButtonEvent(FlyButtonCode, ButtonPressType.JustDown).Listen(Fly);
  }

  void OnDisable() {
    InputManager.ButtonEvent(FlyButtonCode, ButtonPressType.JustDown).Unlisten(Fly);
  }

  void Fly() {
    Debug.Log("Fly");
    InputManager.Consume(FlyButtonCode, ButtonPressType.JustDown);
    SystemTester.SetActiveState(SystemTester.AirborneState);
  }
}