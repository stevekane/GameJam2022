using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class SimpleAbility : MonoBehaviour {
  public AbilityTag Tags;
  public AbilityTag BlockActionsWith;
  public AbilityTag AddedToOwner;
  public SimpleAbilityManager SimpleAbilityManager { get; private set; }
  [field:SerializeField]
  public virtual bool IsRunning { get; set; }
  public virtual void Stop() {}

  void OnEnable() {
    SimpleAbilityManager = GetComponentInParent<SimpleAbilityManager>();
    SimpleAbilityManager.Abilities.Add(this);
  }

  void OnDisable() {
    SimpleAbilityManager.Abilities.Remove(this);
  }
}

public abstract class SimpleAbilityVector3 : SimpleAbility {
  public Vector3 Value { get; set; }
}