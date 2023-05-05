using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class SimpleAbility : MonoBehaviour {
  public AbilityTag BlockActionsWith;
  public AbilityTag Tags;
  public AbilityTag AddedToOwner;
  public virtual bool IsRunning { get; set; }
  public virtual void Stop() {
    Tags = default;
    AddedToOwner = default;
    IsRunning = false;
  }

  protected SimpleAbilityManager AbilityManager;

  void OnEnable() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    AbilityManager.AddAbility(this);
  }

  void OnDisable() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    if (AbilityManager)
      AbilityManager.RemoveAbility(this);
  }
}