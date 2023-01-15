using System.Collections.Generic;
using System.Threading.Tasks;

public class CycleAbility : Ability {
  public List<Ability> Abilities;
  int CycleIndex = -1;

  public override AbilityTag ActiveTags => Abilities[CycleIndex].ActiveTags;

  public override async Task MainAction(TaskScope scope) {
    CycleIndex = (CycleIndex + 1) % Abilities.Count;
    AbilityMethod method = Abilities[CycleIndex].MainAction;
    await AbilityManager.TryRun(scope, method);
  }
}
