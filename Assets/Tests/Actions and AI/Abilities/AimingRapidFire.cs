using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
  public class AimingRapidFire : SimpleAbility {
    [SerializeField] Timeval Cooldown = Timeval.FromSeconds(.25f);
    [SerializeField] Transform Muzzle;
    [SerializeField] GameObject BulletPrefab;

    TaskScope Scope = new();

    public override void OnRun() {
      if (Scope != null) {
        Scope.Dispose();
      }
      Scope = new();
      Scope.Run(Fire);
    }

    public override void OnStop() {
      if (Scope != null) {
        Scope.Dispose();
      }
      Scope = null;
    }

    async Task Fire(TaskScope scope) {
      while (true) {
        await scope.Ticks(Cooldown.Ticks);
        Instantiate(BulletPrefab, Muzzle.position, Muzzle.rotation);
      }
    }
  }
}