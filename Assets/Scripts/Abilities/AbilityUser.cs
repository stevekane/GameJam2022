using System.Collections.Generic;
using UnityEngine;

public class AbilityUser : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;

  // TODO: some kind of disjoint union would be preferred
  Dictionary<string, (ButtonCode, ButtonPressType)> TagToButton = new();
  Dictionary<string, EventSource> TagToEvent = new();

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

  public void RegisterTag(string name, ButtonCode code, ButtonPressType type) {
    Debug.Assert(!TagToEvent.ContainsKey(name));
    TagToButton[name] = (code, type);
  }
  public void RegisterTag(string name, EventSource source) {
    Debug.Assert(!TagToButton.ContainsKey(name));
    TagToEvent[name] = source;
  }
  public EventSource GetEvent(string name) {
    if (TagToEvent.TryGetValue(name, out EventSource evt))
      return evt;
    if (TagToButton.TryGetValue(name, out var button))
      return InputManager.Instance.ButtonEvent(button.Item1, button.Item2);
    return null;
  }
}
