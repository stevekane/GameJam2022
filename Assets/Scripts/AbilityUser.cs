using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
  }

  public Ability TryStartAbility(Ability ability) {
    if (ability.IsRunning)
      return null;
    ability.Activate();
    return ability;
  }
  public Ability TryStartAbility(int index) => TryStartAbility(Abilities[index]);
  public void StopAllAbilities() => Abilities.ForEach((a) => a.Stop());
}
