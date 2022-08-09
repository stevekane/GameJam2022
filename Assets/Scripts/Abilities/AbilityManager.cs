using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour {
  public List<AbilityActivationBinding> AbilityActivationBindings = new();
  public List<EventSource> ActivationEventSources = new();
  public List<AbilityActionMap> AbilityActionMaps = new();

  public List<Ability> Abilities = new();
  public List<Ability> ToAdd = new();

  public EventSource L1JustDown = new();
  public EventSource L1JustUp = new();
  public EventSource L2JustDown = new();
  public EventSource L2JustUp = new();
  public EventSource R1JustDown = new();
  public EventSource R1JustUp = new();
  public EventSource R2JustDown = new();
  public EventSource R2JustUp = new();
  public EventSource NorthJustDown = new();
  public EventSource NorthJustUp = new();
  public EventSource EastJustDown = new();
  public EventSource EastJustUp = new();
  public EventSource SouthJustDown = new();
  public EventSource SouthJustUp = new();
  public EventSource WestJustDown = new();
  public EventSource WestJustUp = new();

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
    }
  }

  // NOTE: This just runs on enable and thus won't work if you add bindings
  // at run-time. This should probably become a set of methods that works
  // more or less like grant abilities or whatever... fucking stateful
  // programming everywhere you look.
  void OnEnable() {
    foreach (var binding in AbilityActivationBindings) {
      var es = new EventSource {
        Action = delegate {
          Debug.Log("Hi");
          TryRun(binding.Ability);
        }
      };
      binding.InputAction.Connected.Add(es);
      ActivationEventSources.Add(es);
    }
  }

  void OnDisable() {
    for (var i = 0; i < ActivationEventSources.Count; i++) {
      var source = ActivationEventSources[i];
      var binding = AbilityActivationBindings[i];
      binding.InputAction.Connected.Remove(source);
    }
    ActivationEventSources.Clear();
  }

  // TODO: Should this be manually set high in Script Execution Order instead?
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