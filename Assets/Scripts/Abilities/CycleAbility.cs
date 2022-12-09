using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CycleAbility : Ability {
  public List<AbilityMethodReference> Abilities;
  AbilityMethodTask[] AbilityMethods;
  int CycleIndex = 0;

  public async Task Attack(TaskScope scope) {
    var (ability, method) = (Abilities[CycleIndex].Ability, AbilityMethods[CycleIndex]);
    CycleIndex = (CycleIndex + 1) % AbilityMethods.Length;
    await AbilityManager.TryRun(scope, method);
  }

  void Start() {
    AbilityMethods = Abilities.Select(a => a.GetMethodTask()).ToArray();
  }
}
