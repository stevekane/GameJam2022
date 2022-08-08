using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour {
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