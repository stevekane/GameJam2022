using System;
using System.Threading.Tasks;

[Serializable]
public class AbilityAction {
  public AbilityTag Tags;
  public AbilityTag CancelAbilitiesWith;
  public AbilityTag AddToOwner;
  public AbilityTag AddToAbility;
  public AbilityTag OwnerActivationRequired;
  public AbilityTag OwnerActivationBlocked;
  public EventSource Source = new();
  public bool CanRun;
  public SimpleAbility Ability;
  public async Task ListenFor(TaskScope scope) {
    try {
      CanRun = true;
      await scope.ListenFor(Source);
    } finally {
      CanRun = false;
    }
  }
}