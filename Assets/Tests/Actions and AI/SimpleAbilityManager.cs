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
    var ownerTagsAfterCancelations = RemoveTagsFromCancellable(ability, Tags);
    var ownerAllowed = ownerTagsAfterCancelations.HasAllFlags(ability.Tags.OwnerActivationRequired);
    var ownerBlocked = ownerTagsAfterCancelations.HasAnyFlags(ability.Tags.OwnerActivationBlocked);
    var abilityBlocked = Abilities.Any(a => IsBlocked(ability, a));
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
      if (IsCancellable(ability, otherAbility))
        Stop(otherAbility);
    }
    Tags.AddFlags(ability.Tags.OwnerWhileActive);
    ability.Tags.Current = ability.Tags.OnStart;
    ability.OnRun();
  }

  public void Stop(SimpleAbility ability) {
    Tags.ClearFlags(ability.Tags.OwnerWhileActive);
    if (ability.IsRunning) {
      ability.OnStop();
    }
  }

  AbilityTag RemoveTagsFromCancellable(SimpleAbility ability, AbilityTag tags) {
    foreach (var otherAbility in Abilities) {
      if (IsCancellable(ability, otherAbility))
        tags.ClearFlags(otherAbility.Tags.OwnerWhileActive);
    }
    return tags;
  }

  // cancellable if running and incoming ability cancels you
  bool IsCancellable(SimpleAbility incomingAbility, SimpleAbility currentAbility) {
    return currentAbility.IsRunning
        && currentAbility.Tags.Current.HasAnyFlags(incomingAbility.Tags.CancelAbilitiesWith);
  }

  // blocked if a running ability cannot be canceled and blocks you
  bool IsBlocked(SimpleAbility incomingAbility, SimpleAbility currentAbility) {
    return currentAbility.IsRunning
        && currentAbility.Tags.Current.HasAnyFlags(incomingAbility.Tags.CancelAbilitiesWith)
        && currentAbility.Tags.BlockAbilitiesWith.HasAnyFlags(incomingAbility.Tags.Current);
  }
}