using System.Threading.Tasks;
using UnityEngine;

namespace Archero {
  public class AbilitySequence : ClassicAbility {
    public AbilityActionFieldReference[] Abilities;
    public float Delay;

    public override async Task MainAction(TaskScope scope) {
      try {
        foreach (var a in Abilities) {
          var action = a.Value;
          Debug.Log($"Run {action.Ability}");
          AbilityManager.Run(action);
          await scope.While(() => action.Ability.IsRunning);
          await scope.Seconds(Delay);
        }
      } finally {
      }
    }
  }
}