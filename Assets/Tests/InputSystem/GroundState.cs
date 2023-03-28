using UnityEngine;
using UnityEngine.InputSystem;

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

  void FixedUpdate() {
    var position = transform.position;
    position.y = Mathf.MoveTowards(position.y, 0, Time.deltaTime);
    transform.position = position;
  }

  void Fly() {
    Debug.Log("Fly");
    InputManager.Consume(FlyButtonCode, ButtonPressType.JustDown);
    SystemTester.SetActiveState(SystemTester.AirborneState);
  }
}