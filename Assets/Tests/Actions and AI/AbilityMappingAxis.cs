using UnityEngine;

public class AbilityMappingAxis : MonoBehaviour {
  [SerializeField] AxisProcessor AxisProcessor;
  [SerializeField] SimpleAbility SimpleAbility;
  [SerializeField] AxisCode AxisCode;

  InputManager InputManager;
  SimpleAbilityManager SimpleAbilityManager;

  void Awake() {
    InputManager = GetComponentInParent<InputManager>();
    SimpleAbilityManager = GetComponentInParent<SimpleAbilityManager>();
  }

  void TryRun(AxisState axisState) {
    AxisProcessor.Process(axisState);
    SimpleAbilityManager.TryRun(SimpleAbility);
  }

  void Stop() {
    SimpleAbilityManager.Stop(SimpleAbility);
  }

  void OnEnable() {
    InputManager?.AxisEvent(AxisCode).Listen(TryRun);
  }

  void OnDisable() {
    InputManager?.AxisEvent(AxisCode).Unlisten(TryRun);
  }
}