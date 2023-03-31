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

    void OnEnable() {
      Scope = new();
      StartAimAction.IsAvailable = true;
      StopAimAction.IsAvailable = true;
      UpdateAimAction.IsAvailable = false;
    }

    void OnDisable() {
      Scope.Dispose();
      StartAimAction.IsAvailable = false;
      StopAimAction.IsAvailable = false;
      UpdateAimAction.IsAvailable = false;
    }

    async Task Fire(TaskScope scope) {
      try {
        UpdateAimAction.IsAvailable = true;
        await scope.Repeat(async delegate {
          await scope.Ticks(Cooldown.Ticks);
          Instantiate(BulletPrefab, Muzzle.position, Muzzle.rotation);
        });
      } finally {
        UpdateAimAction.IsAvailable = false;
      }
    }

    public void StartAim() {
      Scope.Run(Fire);
    }

    public void StopAim() {
      Scope.Dispose();
      Scope = new();
    }

    public void UpdateAim(Vector3 v) {
      var xz = v.XZ();
      if (xz.sqrMagnitude > 0)
        Owner.rotation = Quaternion.LookRotation(xz.normalized, Vector3.up);
    }
  }
}