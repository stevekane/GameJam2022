using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ability : MonoBehaviour {
  Fiber? Routine;
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public List<AbilityAction> ConsumedActions = new();
  public bool IsRunning { get => Routine.HasValue; }
  public virtual void Activate() => Routine = new Fiber(MakeRoutine());
  public virtual void Stop() => Routine = null;
  public virtual void OnAbilityAction(AbilityManager manager, AbilityAction action) {}
  protected abstract IEnumerator MakeRoutine();
  void FixedUpdate() => Routine?.MoveNext();
}

public abstract class ChargedAbility : Ability {
  public virtual void ReleaseCharge() { }
}