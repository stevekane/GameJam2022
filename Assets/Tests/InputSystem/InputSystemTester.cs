using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Input+1)]
public class InputSystemTester : MonoBehaviour {
  [SerializeField] InputManager InputManager;
  [SerializeField] InputMappings InputMappings;

  public GroundState GroundState;
  public AirborneState AirborneState;
  public MonoBehaviour ActiveState;
  public List<MonoBehaviour> ActiveAbilities;
  public TaskScope AbilityScope = new();

  public void StopAbility(MonoBehaviour ability) {
    ActiveAbilities.Remove(ability);
    ActiveState.enabled = false;
    ActiveState.enabled = true;
  }

  public void StartAbility(MonoBehaviour ability) {
    ActiveAbilities.Add(ability);
  }

  public MonoBehaviour SetActiveState(MonoBehaviour state) {
    if (ActiveState)
      ActiveState.enabled = false;
    ActiveState = state;
    ActiveState.enabled = true;
    return state;
  }

  void Start() {
    GroundState.enabled = false;
    AirborneState.enabled = false;
    SetActiveState(GroundState);
  }

  void OnDestroy() {
    Destroy(GroundState);
    Destroy(AirborneState);
    AbilityScope.Dispose();
  }
}