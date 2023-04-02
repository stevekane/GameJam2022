using System.Threading.Tasks;
using UnityEngine;

namespace ActionsAndAI {
  public class AIAimingRapidFireController : MonoBehaviour {
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
          for (var i = 0; i < 20; i++) {
            await scope.Tick();
          }
          await scope.Ticks(40);
        });
      } finally {
      }
    }
  }
}