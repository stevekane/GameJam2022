using UnityEngine;

public class SampleAbilityRunningCondition : Condition {
  [SerializeField] SampleAbility Ability;
  [SerializeField] bool IsRunning = true;

  public override bool Satisfied => Ability.IsRunning == IsRunning;
}