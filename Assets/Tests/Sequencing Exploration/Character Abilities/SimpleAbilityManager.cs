using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class SimpleAbilityManager : MonoBehaviour {
  AbilityTag NextSystemTags;
  AbilityTag SystemTags;

  [HideInInspector, NonSerialized]
  public List<SimpleAbility> Abilities = new();

  [field:SerializeField]
  public AbilityTag Tags { get; private set; }

  public bool HasTag(AbilityTag tag) => (NextSystemTags & tag) != 0;
  public void AddTag(AbilityTag tag) => NextSystemTags |= tag;
  public void RemoveTag(AbilityTag tag) => NextSystemTags.ClearFlags(tag);
  public void SetTag(AbilityTag tag, bool active) {
    if (active) AddTag(tag);
    else        RemoveTag(tag);
  }

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

  public bool CanRun<T>(AbilityAction<T> action) {
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

  public void Run<T>(AbilityAction<T> action, T t) {
    foreach (var ability in Abilities)
      if (ability.IsRunning && IsCancellable(action, ability))
        ability.Stop();
    action.Fire(t);
  }

  void OnDestroy() => Abilities.ForEach(a => a.Stop());

  void FixedUpdate() {
    SystemTags = NextSystemTags;
    Tags = AbilityOwnerTagsWhere(a => a.IsRunning, SystemTags);
  }

  bool IsCancellable(AbilityAction action, SimpleAbility ability) {
    var hasAll = action.CancelAbilitiesWithAll != default && ability.Tags.HasAllFlags(action.CancelAbilitiesWithAll);
    var hasAny = ability.Tags.HasAnyFlags(action.CancelAbilitiesWithAny);
    return hasAll || hasAny;
  }

  bool IsCancellable<T>(AbilityAction<T> action, SimpleAbility ability) {
    var hasAll = action.CancelAbilitiesWithAll != default && ability.Tags.HasAllFlags(action.CancelAbilitiesWithAll);
    var hasAny = ability.Tags.HasAnyFlags(action.CancelAbilitiesWithAny);
    return hasAll || hasAny;
  }

  bool IsBlocked(AbilityAction action, SimpleAbility ability) {
    return ability.BlockActionsWith.HasAnyFlags(action.Tags);
  }

  bool IsBlocked<T>(AbilityAction<T> action, SimpleAbility ability) {
    return ability.BlockActionsWith.HasAnyFlags(action.Tags);
  }

  AbilityTag AbilityOwnerTagsWhere(Predicate<SimpleAbility> predicate, AbilityTag tag = default) {
    return Abilities.Aggregate(tag, (tags, ability) => tags | (predicate(ability) ? ability.AddedToOwner : default));
  }
}