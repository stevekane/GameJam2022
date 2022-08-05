using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour {
  // TODO: Abilities and ToAdd should be stacks but not visible in editor is qq
  public List<Ability> Abilities = new();
  public List<Ability> ToAdd = new();

  public void TryRun(Ability ability) {
    var notAlreadyRunning = !ability.IsRunning;
    var notBlocked = Abilities.TrueForAll(a => (a.Blocks & ability.Tags) == 0);
    if (notAlreadyRunning && notBlocked) {
      ability.Activate();
      ToAdd.Add(ability);
      foreach (var activeAbility in Abilities) {
        if ((activeAbility.Cancels & ability.Tags) != 0) {
          activeAbility.Stop();
        }
      }
    } else {
      Debug.Log("Blocked!");
    }
  }

  public void OnAbilityAction(AbilityAction action) {
    foreach (var ability in Abilities) {
      if (ability.IsRunning && ability.ConsumedActions.Contains(action)) {
        Debug.Log($"{ability.name} consumed {action}");
        ability.OnAbilityAction(this, action);
      }
    }
  }

  // TODO: Late Update? Should this be manually set high in Script Execution Order instead?
  void LateUpdate() {
    Abilities.RemoveAll(a => !a.IsRunning);
    StackAdd(Abilities, ToAdd);
    ToAdd.Clear();
  }

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }
}