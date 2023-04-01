using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAbilityManager : MonoBehaviour {
  public AbilityTag Tags;
  public List<SimpleAbility> Abilities;

  void OnDestroy() {
    Abilities.ForEach(Stop);
  }

  public bool CanRun(SimpleAbility ability) {
    var conditionsSatisfied = ability.Conditions.All(c => c.Satisfied);
    var ownerAllowed = Tags.HasAllFlags(ability.Tags.OwnerActivationRequired);
    var ownerBlocked = Tags.HasAnyFlags(ability.Tags.OwnerActivationBlocked);
    var abilityBlocked = Abilities.Any(a => a.IsRunning && a.Tags.BlockAbilitiesWith.HasAnyFlags(ability.Tags.Current));
    var shouldStart = conditionsSatisfied && ownerAllowed && !ownerBlocked && !abilityBlocked;
    return shouldStart;
  }

  public bool TryRun(SimpleAbility ability) {
    var canRun = CanRun(ability);
    if (canRun) {
      Run(ability);
    }
    return canRun;
  }

  public void Run(SimpleAbility ability) {
    foreach (var otherAbility in Abilities) {
      if (otherAbility.IsRunning && otherAbility.Tags.Current.HasAnyFlags(ability.Tags.CancelAbilitiesWith)) {
        Stop(otherAbility);
      }
    }
    Tags.AddFlags(ability.Tags.OwnerWhileActive);
    ability.IsRunning = true;
    ability.OnRun();
  }

  public void Stop(SimpleAbility ability) {
    Tags.ClearFlags(ability.Tags.OwnerWhileActive);
    if (ability.IsRunning) {
      ability.IsRunning = false;
      ability.OnStop();
    }
  }
}