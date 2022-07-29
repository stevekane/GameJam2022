using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
  }

  public Ability TryStartAbility(Ability ability) {
    if (ability.IsComplete) {
      ability.Begin();
      return ability;
    } else {
      return null;
    }
  }
  public Ability TryStartAbility(int index) {
    return TryStartAbility(Abilities[index]);
  }
  public void StopAllAbilities() {
    foreach (var a in Abilities) {
      if (!a.IsComplete)
        a.End();
    }
  }
}
