using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class SimpleAbilityManager : MonoBehaviour {
  AbilityTag NextSystemTags;
  AbilityTag SystemTags;
  List<SimpleAbility> Abilities = new();

  [field:SerializeField]
  public AbilityTag Tags { get; private set; }

  public void AddTag(AbilityTag tag) => NextSystemTags |= tag;
  public void RemoveTag(AbilityTag tag) => NextSystemTags.ClearFlags(tag);

  public void AddAbility(SimpleAbility ability) => Abilities.Add(ability);
  public void RemoveAbility(SimpleAbility ability) {
    ability.Stop();
    Abilities.Remove(ability);
  }

  public bool CanRun(AbilityAction action) {
    var predicateSatisfied = action.CanRun;
    var ownerTagsAfterCancelations = AbilityOwnerTagsWhere(a => a.IsRunning && !IsCancellable(action, a), SystemTags);
    var ownerAllowed = ownerTagsAfterCancelations.HasAllFlags(action.OwnerActivationRequired);
    var ownerBlocked = ownerTagsAfterCancelations.HasAnyFlags(action.OwnerActivationBlocked);
    var abilityBlocked = Abilities.Any(a => a.IsRunning && !IsCancellable(action, a) && IsBlocked(action, a));
    return predicateSatisfied && ownerAllowed && !ownerBlocked && !abilityBlocked;
  }

  public void Run(AbilityAction action) {
    foreach (var ability in Abilities)
      if (ability.IsRunning && IsCancellable(action, ability))
        ability.Stop();
    action.Fire();
  }

  void OnDestroy() => Abilities.ForEach(a => a.Stop());

  void FixedUpdate() {
    SystemTags = NextSystemTags;
    Tags = AbilityOwnerTagsWhere(a => a.IsRunning, SystemTags);
  }

  bool IsCancellable(AbilityAction action, SimpleAbility ability) {
    return ability.Tags.HasAnyFlags(action.CancelAbilitiesWith);
  }

  bool IsBlocked(AbilityAction action, SimpleAbility ability) {
    return ability.BlockActionsWith.HasAnyFlags(action.Tags);
  }

  AbilityTag AbilityOwnerTagsWhere(Predicate<SimpleAbility> predicate, AbilityTag tag = default) {
    return Abilities.Aggregate(tag, (tags, ability) => tags | (predicate(ability) ? ability.AddedToOwner : default));
  }
}