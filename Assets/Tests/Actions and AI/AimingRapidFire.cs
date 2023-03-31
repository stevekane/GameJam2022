using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
  public class AimingRapidFire : MonoBehaviour {
    [SerializeField] Timeval Cooldown = Timeval.FromSeconds(.25f);
    [SerializeField] Transform Muzzle;
    [SerializeField] Transform Owner;
    [SerializeField] GameObject BulletPrefab;
    [SerializeField] Aiming Aiming;
    [SerializeField] ActionEventSource StartAimAction;
    [SerializeField] ActionEventSource StopAimAction;
    [SerializeField] ActionEventSourceVector3 UpdateAimAction;

    TaskScope Scope = new();

    async Task Fire(TaskScope scope) {
      try {
        StopAimAction.IsAvailable = true;
        UpdateAimAction.IsAvailable = true;
        Aiming.Value = true;
        await scope.Repeat(async delegate {
          await scope.Ticks(Cooldown.Ticks);
          Instantiate(BulletPrefab, Muzzle.position, Muzzle.rotation);
        });
      } finally {
        Aiming.Value = false;
        StopAimAction.IsAvailable = false;
        UpdateAimAction.IsAvailable = false;
      }
    }

    public void StartAim() {
      Scope = new();
      Scope.Run(Fire);
    }

    public void StopAim() {
      Scope?.Dispose();
    }

    public void UpdateAim(Vector3 v) {
      var xz = v.XZ();
      if (xz.sqrMagnitude > 0)
        Owner.rotation = Quaternion.LookRotation(xz.normalized, Vector3.up);
    }
  }
}