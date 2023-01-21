using System.Collections.Generic;
using System.Threading.Tasks;

public class CycleAbility : Ability {
  public List<Ability> Abilities;
  int CycleIndex = 0;
  int NextIndex => (CycleIndex + 1) % Abilities.Count;

  public override AbilityTag ActiveTags => Tags | Abilities[CycleIndex].ActiveTags;

  public override bool CanStart(AbilityMethod func) => Abilities[NextIndex].CanStart(Abilities[NextIndex].MainAction);
  public override async Task MainAction(TaskScope scope) {
    CycleIndex = NextIndex;
    AbilityMethod method = Abilities[CycleIndex].MainAction;
    await Abilities[CycleIndex].Run(scope, method);
  }
}
