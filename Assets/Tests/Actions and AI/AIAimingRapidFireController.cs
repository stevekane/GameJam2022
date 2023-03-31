using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  public class AIAimingRapidFireController : MonoBehaviour {
    [SerializeField] ActionEventSource StartAimAction;
    [SerializeField] ActionEventSource StopAimAction;
    [SerializeField] ActionEventSourceVector3 UpdateAimAction;

    TaskScope Scope;

    void OnEnable() {
      Scope = new();
      Scope.Run(Brain);
    }

    void OnDisable() {
      Scope.Dispose();
    }

    async Task Brain(TaskScope scope) {
      try {
        await scope.Repeat(async delegate {
          StartAimAction.TryFire();
          for (var i = 0; i < 20; i++) {
            UpdateAimAction.TryFire(Random.onUnitSphere.XZ().normalized);
            await scope.Tick();
          }
          StopAimAction.TryFire();
          await scope.Ticks(40);
        });
      } finally {
        StopAimAction.TryFire();
      }
    }
  }
}