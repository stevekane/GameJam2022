using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CycleAbility : Ability {
  public List<AbilityMethodReference> Abilities;
  AbilityMethod[] AbilityMethods;
  int CycleIndex = 0;

  public IEnumerator Attack() {
    var (ability, method) = (Abilities[CycleIndex].Ability, AbilityMethods[CycleIndex]);
    CycleIndex = (CycleIndex + 1) % AbilityMethods.Length;
    AbilityManager.GetEvent(method).Fire();
    yield return Fiber.Until(() => !ability.IsRunning);
  }

  void Start() {
    AbilityMethods = Abilities.Select(a => a.GetMethod()).ToArray();
  }
}
