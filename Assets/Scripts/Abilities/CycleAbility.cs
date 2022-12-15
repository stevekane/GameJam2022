using System.Collections.Generic;
using System.Threading.Tasks;

public class CycleAbility : Ability {
  public List<Ability> Abilities;
  int CycleIndex = 0;

  public override async Task MainAction(TaskScope scope) {
    AbilityMethod method = Abilities[CycleIndex].MainAction;
    CycleIndex = (CycleIndex + 1) % Abilities.Count;
    await AbilityManager.TryRun(scope, method);
  }
}
