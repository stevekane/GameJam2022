using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class SimpleAbility : MonoBehaviour {
  public AbilityTag BlockActionsWith;
  public AbilityTag Tags;
  public AbilityTag AddedToOwner;
  [field:SerializeField]
  public virtual bool IsRunning { get; set; }
  public virtual void Stop() {
    Tags = default;
    AddedToOwner = default;
    IsRunning = false;
  }

  protected SimpleAbilityManager AbilityManager;

  void Start() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    AbilityManager.AddAbility(this);
  }

  void OnDestroy() {
    AbilityManager = GetComponentInParent<SimpleAbilityManager>();
    AbilityManager.RemoveAbility(this);
  }
}

public abstract class SimpleAbilityVector3 : SimpleAbility {
  public Vector3 Value { get; set; }
}