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
    void OnDisable() => Scope.Dispose();

    async Task Brain(TaskScope scope) {
      try {
        await scope.Repeat(async delegate {
          if (StartAimAction.IsAvailable)
            StartAimAction.Fire();
          for (var i = 0; i < 20; i++) {
            if (UpdateAimAction.IsAvailable)
              UpdateAimAction.Fire(Random.onUnitSphere.XZ().normalized);
            await scope.Tick();
          }
          if (StopAimAction.IsAvailable)
            StopAimAction.Fire();
          await scope.Ticks(40);
        });
      } finally {
        if (StopAimAction.IsAvailable)
          StopAimAction.Fire();
      }
    }
  }
}