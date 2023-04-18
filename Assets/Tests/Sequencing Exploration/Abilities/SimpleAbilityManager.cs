using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class SimpleAbilityManager : MonoBehaviour {
  AbilityTag NextSystemTags;
  AbilityTag SystemTags;
  [field:SerializeField]
  public AbilityTag Tags { get; private set; }
  public List<SimpleAbility> Abilities;

  void OnDestroy() {
    Abilities.ForEach(a => a.Stop());
  }

  void FixedUpdate() {
    SystemTags = NextSystemTags;
    Tags = SystemTags | AbilityOwnerTagsWhere(a => a.IsRunning);
  }

  public void AddTag(AbilityTag tag) {
    NextSystemTags |= tag;
  }

  public void RemoveTag(AbilityTag tag) {
    NextSystemTags.ClearFlags(tag);
  }

  public bool TryRun(AbilityAction action) {
    var canRun = CanRun(action);
    if (canRun)
      Run(action);
    return canRun;
  }

  public bool CanRun(AbilityAction action) {
    var predicateSatisfied = action.CanRun;
    var ownerTagsAfterCancelations = SystemTags | AbilityOwnerTagsWhere(a => a.IsRunning && !IsCancellable(action, a));
    var ownerAllowed = ownerTagsAfterCancelations.HasAllFlags(action.OwnerActivationRequired);
    var ownerBlocked = ownerTagsAfterCancelations.HasAnyFlags(action.OwnerActivationBlocked);
    var abilityBlocked = Abilities.Any(a => a.IsRunning && !IsCancellable(action, a) && IsBlocked(action, a));
    return predicateSatisfied && ownerAllowed && !ownerBlocked && !abilityBlocked;
  }

  public void Run(AbilityAction action) {
    foreach (var ability in Abilities) {
      if (ability.IsRunning && IsCancellable(action, ability))
        ability.Stop();
    }
    action.Fire();
  }

  bool IsCancellable(AbilityAction action, SimpleAbility ability) {
    return ability.Tags.HasAnyFlags(action.CancelAbilitiesWith);
  }

  bool IsBlocked(AbilityAction action, SimpleAbility ability) {
    return ability.BlockActionsWith.HasAnyFlags(action.Tags);
  }

  AbilityTag AbilityOwnerTagsWhere(Predicate<SimpleAbility> predicate) {
    AbilityTag tag = default;
    foreach (var ability in Abilities)
      if (predicate(ability))
        tag |= ability.AddedToOwner;
    return tag;
  }
}