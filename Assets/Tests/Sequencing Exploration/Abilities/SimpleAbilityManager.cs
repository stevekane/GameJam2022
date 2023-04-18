using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class SimpleAbilityManager : MonoBehaviour {
  AbilityTag NextTags;
  [field:SerializeField]
  public AbilityTag Tags { get; private set; }
  public List<SimpleAbility> Abilities;

  void OnDestroy() {
    Abilities.ForEach(Stop);
  }

  void FixedUpdate() {
    Tags = NextTags | TagsWhere(a => a.IsRunning);
    NextTags = default;
  }

  public void AddTag(AbilityTag tag) {
    NextTags |= tag;
  }

  public bool TryRun(AbilityAction action) {
    var canRun = CanRun(action);
    if (canRun)
      Run(action);
    return canRun;
  }

  public bool CanRun(AbilityAction action) {
    var predicateSatisfied = action.CanRun;
    var ownerTagsAfterCancelations = TagsWhere(a => a.IsRunning && IsCancellable(action, a));
    var ownerAllowed = ownerTagsAfterCancelations.HasAllFlags(action.OwnerActivationRequired);
    var ownerBlocked = ownerTagsAfterCancelations.HasAnyFlags(action.OwnerActivationBlocked);
    var abilityBlocked = Abilities.Any(a => a.IsRunning && !IsCancellable(action, a) && IsBlocked(action, a));
    return predicateSatisfied && ownerAllowed && !ownerBlocked && !abilityBlocked;
  }

  public void Run(AbilityAction action) {
    foreach (var ability in Abilities) {
      if (ability.IsRunning && IsCancellable(action, ability))
        Stop(ability);
    }
    action.Ability.Tags.AddFlags(action.AddToAbility);
    action.Source.Fire();
  }

  public void Stop(SimpleAbility ability) {
    if (ability.IsRunning) {
      ability.Tags = default;
      ability.Stop();
    }
  }

  bool IsCancellable(AbilityAction action, SimpleAbility ability) {
    return ability.Tags.HasAnyFlags(action.CancelAbilitiesWith);
  }

  bool IsBlocked(AbilityAction action, SimpleAbility ability) {
    return ability.BlockActionsWith.HasAnyFlags(action.Tags);
  }

  AbilityTag TagsWhere(Predicate<SimpleAbility> predicate) {
    AbilityTag tag = default;
    foreach (var ability in Abilities) {
      if (predicate(ability))
        continue;
      tag |= ability.AddedToOwner;
    }
    return tag;
  }
}