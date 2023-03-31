using UnityEngine;

public class ButtonAbilityMapping : MonoBehaviour {
  [SerializeField] ButtonCode StartButtonCode;
  [SerializeField] ButtonPressType StartButtonPressType;
  [SerializeField] ButtonCode StopButtonCode;
  [SerializeField] ButtonPressType StopButtonPressType;
  [SerializeField] SimpleAbility SimpleAbility;
  [SerializeField] bool ConsumeStartOnFire;
  [SerializeField] bool ConsumeStopOnFire;

  InputManager InputManager;
  SimpleAbilityManager SimpleAbilityManager;

  void Awake() {
    InputManager = GetComponentInParent<InputManager>();
    SimpleAbilityManager = GetComponentInParent<SimpleAbilityManager>();
  }

  void TryRun() {
    if (SimpleAbilityManager.TryRun(SimpleAbility) && ConsumeStartOnFire)
      InputManager.Consume(StartButtonCode, StartButtonPressType);
  }

  void Stop() {
    SimpleAbility.Stop();
    if (ConsumeStopOnFire)
      InputManager.Consume(StopButtonCode, StopButtonPressType);
  }

  void OnEnable() {
    InputManager?.ButtonEvent(StartButtonCode, StartButtonPressType).Listen(TryRun);
    InputManager?.ButtonEvent(StopButtonCode, StopButtonPressType).Listen(Stop);
  }

  void OnDisable() {
    InputManager?.ButtonEvent(StartButtonCode, StartButtonPressType).Unlisten(TryRun);
    InputManager?.ButtonEvent(StopButtonCode, StopButtonPressType).Unlisten(Stop);
  }
}