using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  public class AimingRapidFire : MonoBehaviour {
    [SerializeField] Timeval Cooldown = Timeval.FromSeconds(.25f);
    [SerializeField] Transform Muzzle;
    [SerializeField] Transform Owner;
    [SerializeField] GameObject BulletPrefab;
    [SerializeField] ActionEventSource StartAimAction;
    [SerializeField] ActionEventSource StopAimAction;
    [SerializeField] ActionEventSourceVector3 UpdateAimAction;
    TaskScope Scope = new();

    void OnEnable() {
      Scope = new();
      StartAimAction.IsAvailable = true;
      StopAimAction.IsAvailable = true;
      UpdateAimAction.IsAvailable = false;
      StartAimAction.Set(StartAim);
      StopAimAction.Set(StopAim);
      UpdateAimAction.Set(UpdateAim);
    }

    void OnDisable() {
      Scope.Dispose();
      StartAimAction.IsAvailable = false;
      StopAimAction.IsAvailable = false;
      UpdateAimAction.IsAvailable = false;
      StartAimAction.Clear();
      StopAimAction.Clear();
      UpdateAimAction.Clear();
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
    public void UpdateAim(Vector3 v) => Owner.rotation = Quaternion.LookRotation(v.XZ().normalized, Vector3.up);
  }
}