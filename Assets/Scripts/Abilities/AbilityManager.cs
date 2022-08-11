using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;

  List<Ability> Running = new();
  List<Ability> ToAdd = new();

  // TODO: some kind of disjoint union would be preferred
  Dictionary<EventTag, (ButtonCode, ButtonPressType)> TagToButton = new();
  Dictionary<EventTag, EventSource> TagToEvent = new();
  Dictionary<EventTag, AxisCode> TagToAxis = new();

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
  }
  void Start() {
    Abilities.ForEach(a => a.Triggers.ForEach(t => t.Init(this, a)));
    Abilities.ForEach(a => a.AbilityManager = this);
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.Triggers.ForEach(t => t.Destroy()));
    Abilities.ForEach(a => a.AbilityManager = null);
  }

  public void TryRun(Ability ability) {
    var notAlreadyRunning = !ability.IsRunning;
    var notBlocked = Running.TrueForAll(a => (a.Blocks & ability.Tags) == 0);
    if (notAlreadyRunning && notBlocked) {
      ability.Activate();
      ToAdd.Add(ability);
      foreach (var activeAbility in Running) {
        if ((activeAbility.Cancels & ability.Tags) != 0) {
          activeAbility.Stop();
        }
      }
    }
  }

  public void StopAllAbilities() => Abilities.ForEach((a) => a.Stop());

  public void RegisterTag(EventTag tag, ButtonCode code, ButtonPressType type) {
    Debug.Assert(!TagToEvent.ContainsKey(tag));
    TagToButton[tag] = (code, type);
  }
  public void RegisterTag(EventTag tag, EventSource evt) {
    Debug.Assert(!TagToButton.ContainsKey(tag));
    TagToEvent[tag] = evt;
  }
  public void RegisterTag(EventTag tag, AxisCode axisCode) {
    TagToAxis[tag] = axisCode;
  }
  public EventSource GetEvent(EventTag tag) {
    if (TagToButton.TryGetValue(tag, out var button))
      return InputManager.Instance.ButtonEvent(button.Item1, button.Item2);
    if (!TagToEvent.TryGetValue(tag, out EventSource evt))
      TagToEvent[tag] = evt = new();
    return evt;
  }
  public AxisState GetAxis(EventTag tag) {
    if (TagToAxis.TryGetValue(tag, out var axisCode)) {
      return InputManager.Instance.Axis(axisCode);
    } else {
      // TODO: Tough call here...what to do/return if there is no existing mapping?
      return null;
    }
  }

  // TODO: Should this be manually set high in Script Execution Order instead?
  void LateUpdate() {
    Running.RemoveAll(a => !a.IsRunning);
    StackAdd(Running, ToAdd);
    ToAdd.Clear();
  }

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }
}
