using UnityEngine;

public class AirborneState : MonoBehaviour {
  [SerializeField] InputSystemTester SystemTester;
  [SerializeField] InputManager InputManager;
  [SerializeField] ButtonCode LandButtonCode;
  [SerializeField] ButtonCode FireButtonCode;
  [SerializeField] TripleFireAbility TripleFireAbility;

  void OnEnable() {
    InputManager.ButtonEvent(LandButtonCode, ButtonPressType.JustDown).Listen(Land);
    // Consume ensures nothing enters the new state's buffer... may or may not want
    InputManager.Consume(FireButtonCode, ButtonPressType.JustDown);
    InputManager.ButtonEvent(FireButtonCode, ButtonPressType.JustDown).Listen(Fire);
  }

  void OnDisable() {
    InputManager.ButtonEvent(LandButtonCode, ButtonPressType.JustDown).Unlisten(Land);
    InputManager.ButtonEvent(FireButtonCode, ButtonPressType.JustDown).Unlisten(Fire);
  }

  void FixedUpdate() {
    var position = transform.position;
    position.y = Mathf.MoveTowards(position.y, 1, Time.deltaTime);
    transform.position = position;
  }

  void Land() {
    Debug.Log("Land");
    InputManager.Consume(LandButtonCode, ButtonPressType.JustDown);
    SystemTester.SetActiveState(SystemTester.GroundState);
  }

  void Fire() {
    InputManager.Consume(FireButtonCode, ButtonPressType.JustDown);
    SystemTester.AbilityScope.Start(TripleFireAbility.Fire);
    SystemTester.StartAbility(TripleFireAbility);
  }
}