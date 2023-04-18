using System;
using System.Threading.Tasks;

[Serializable]
public class AbilityAction : IEventSource {
  EventSource Source = new();
  public AbilityTag Tags;
  public AbilityTag CancelAbilitiesWith;
  public AbilityTag AddToOwner;
  public AbilityTag AddToAbility;
  public AbilityTag OwnerActivationRequired;
  public AbilityTag OwnerActivationBlocked;
  public bool CanRun;
  public SimpleAbility Ability;
  public void Listen(Action handler) => Source.Listen(handler);
  public void Unlisten(Action handler) => Source.Unlisten(handler);
  public void Set(Action handler) => Source.Set(handler);
  public void Clear() => Source.Clear();
  public void Fire() {
    Ability.AddedToOwner.AddFlags(AddToOwner);
    Ability.Tags.AddFlags(AddToAbility);
    Source.Fire();
  }
  public async Task ListenFor(TaskScope scope) {
    try {
      CanRun = true;
      await scope.ListenFor(Source); // TODO: listen to self or source?
    } finally {
      CanRun = false;
    }
  }
}