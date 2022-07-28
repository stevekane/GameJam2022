using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public SimpleAbility[] Abilities;

  void Awake() {
    Abilities = GetComponentsInChildren<SimpleAbility>();
  }

  public SimpleAbility TryStartAbility(SimpleAbility ability) {
    if (ability.IsComplete) {
      ability.Begin();
      return ability;
    } else {
      return null;
    }
  }
  public SimpleAbility TryStartAbility(int index) {
    return TryStartAbility(Abilities[index]);
  }
  public void StopAllAbilities() {
    foreach (var a in Abilities) {
      if (!a.IsComplete)
        a.End();
    }
  }
}
