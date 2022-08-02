using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public AbilityFibered[] Abilities;

  void Awake() {
    Abilities = GetComponentsInChildren<AbilityFibered>();
  }

  public AbilityFibered TryStartAbility(AbilityFibered ability) {
    if (ability.IsRunning)
      return null;
    ability.Activate();
    return ability;
  }
  public AbilityFibered TryStartAbility(int index) => TryStartAbility(Abilities[index]);
  public void StopAllAbilities() => Abilities.ForEach((a) => a.Stop());
}
