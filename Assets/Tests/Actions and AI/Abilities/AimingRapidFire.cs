using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  public class AimingRapidFire : SimpleAbility {
    [SerializeField] Timeval Cooldown = Timeval.FromSeconds(.25f);
    [SerializeField] Transform Muzzle;
    [SerializeField] GameObject BulletPrefab;

    TaskScope Scope;

    public override void OnRun() {
      Scope?.Dispose();
      Scope = new();
      Scope.Run(Fire);
      IsRunning = true;
    }

    public override void OnStop() {
      Scope.Dispose();
      Scope = null;
      IsRunning = false;
    }

    async Task Fire(TaskScope scope) {
      while (true) {
        await scope.Ticks(Cooldown.Ticks);
        Instantiate(BulletPrefab, Muzzle.position, Muzzle.rotation);
      }
    }
  }
}