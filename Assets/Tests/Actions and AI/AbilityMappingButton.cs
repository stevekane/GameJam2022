using UnityEngine;

public class AbilityMappingButton : MonoBehaviour {
  [SerializeField] SimpleAbility SimpleAbility;
  [SerializeField] ButtonCode StartButtonCode;
  [SerializeField] ButtonPressType StartButtonPressType;
  [SerializeField] ButtonCode StopButtonCode;
  [SerializeField] ButtonPressType StopButtonPressType;
  [SerializeField] bool ConsumeStartOnFire;
  [SerializeField] bool ConsumeStartOnStop;
  [SerializeField] bool ConsumeStopOnFire;
  [SerializeField] bool ConsumeStopOnStop;

  InputManager InputManager;
  SimpleAbilityManager SimpleAbilityManager;

  void Awake() {
    InputManager = GetComponentInParent<InputManager>();
    SimpleAbilityManager = GetComponentInParent<SimpleAbilityManager>();
  }

  void TryRun() {
    if (SimpleAbilityManager.TryRun(SimpleAbility)) {
      if (ConsumeStartOnFire)
        InputManager.Consume(StartButtonCode, StartButtonPressType);
      if (ConsumeStopOnFire)
        InputManager.Consume(StopButtonCode, StopButtonPressType);
    }
  }

  void Stop() {
    SimpleAbilityManager.Stop(SimpleAbility);
    if (ConsumeStartOnStop)
      InputManager.Consume(StartButtonCode, StartButtonPressType);
    if (ConsumeStopOnStop)
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