using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAbilityManager : MonoBehaviour {
  public Tags Tags;
  public List<SimpleAbility> Abilities;

  void OnDestroy() {
    Abilities.ForEach(Stop);
  }

  // TODO: Should re-apply the tags from still-running abilities when building
  // the ownerTagsAfterCancellation in case multiple abilities are writing to it
  public bool CanRun(SimpleAbility ability) {
    var conditionsSatisfied = ability.Conditions.All(c => c.Satisfied);
    var ownerTagsAfterCancelations = RemoveTagsFromCancellable(ability, Tags.Current);
    var ownerAllowed = ownerTagsAfterCancelations.HasAllFlags(ability.SimpleTags.OwnerActivationRequired);
    var ownerBlocked = ownerTagsAfterCancelations.HasAnyFlags(ability.SimpleTags.OwnerActivationBlocked);
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
    Tags.Current.AddFlags(ability.SimpleTags.OwnerWhileActive);
    ability.Tags.Current = ability.SimpleTags.OnStart;
    ability.OnRun();
  }

  public void Stop(SimpleAbility ability) {
    if (ability.IsRunning) {
      Tags.Current = default;
      ability.OnStop();
    }
  }

  AbilityTag RemoveTagsFromCancellable(SimpleAbility ability, AbilityTag tags) {
    foreach (var otherAbility in Abilities) {
      if (IsCancellable(ability, otherAbility))
        tags.ClearFlags(otherAbility.SimpleTags.OwnerWhileActive);
    }
    return tags;
  }

  // cancellable if running and incoming ability cancels you
  bool IsCancellable(SimpleAbility incomingAbility, SimpleAbility currentAbility) {
    return currentAbility.IsRunning
        && currentAbility.Tags.Current.HasAnyFlags(incomingAbility.SimpleTags.CancelAbilitiesWith);
  }

  // blocked if a running ability cannot be canceled and blocks you
  bool IsBlocked(SimpleAbility incomingAbility, SimpleAbility currentAbility) {
    return currentAbility.IsRunning
        && currentAbility.Tags.Current.HasAnyFlags(incomingAbility.SimpleTags.CancelAbilitiesWith)
        && currentAbility.SimpleTags.BlockAbilitiesWith.HasAnyFlags(incomingAbility.Tags.Current);
  }
}