using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class SimpleAbility : MonoBehaviour {
  public SimpleTags Tags { get; private set; }
  public Condition[] Conditions { get; private set; }
  public SimpleAbilityManager SimpleAbilityManager { get; private set; }
  [field:SerializeField]
  public virtual bool IsRunning { get; set; }
  public void Run() => SimpleAbilityManager.Run(this);
  public void Stop() => SimpleAbilityManager.Stop(this);
  public virtual void OnRun() {}
  public virtual void OnStop() {}

  void OnEnable() {
    Tags = GetComponent<SimpleTags>();
    Conditions = GetComponents<Condition>();
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