using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;
  [HideInInspector] public AbilityFibered[] AbilitiesF;

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
    AbilitiesF = GetComponentsInChildren<AbilityFibered>();
  }

  public Ability TryStartAbility(Ability ability) {
    if (ability.IsComplete) {
      ability.Begin();
      return ability;
    } else {
      return null;
    }
  }
  public AbilityFibered TryStartAbility(AbilityFibered ability) {
    if (ability.IsRunning)
      return null;
    ability.Activate();
    return ability;
  }
  public Ability TryStartAbility(int index) {
    return TryStartAbility(Abilities[index]);
  }
  public AbilityFibered TryStartAbilityF(int index) {
    return TryStartAbility(AbilitiesF[index]);
  }
  public void StopAllAbilities() {
    foreach (var a in Abilities) {
      if (!a.IsComplete)
        a.End();
    }
  }
}
