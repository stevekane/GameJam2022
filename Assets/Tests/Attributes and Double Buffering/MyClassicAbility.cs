using System.Threading.Tasks;
using UnityEngine;

public class MyClassicAbility : Ability {
  public override bool CanStart(AbilityMethod func) {
    return func switch {
      AbilityMethod m when m == MainAction => !IsRunning,
      AbilityMethod m when m == MainRelease => IsRunning,
      AbilityMethod m when m == Squeak => IsRunning,
      _ => false
    };
  }

  public override async Task MainAction(TaskScope scope) {
    var start = Timeval.TickCount;
    var max = 120;
    Debug.Log("Start charging");
    await ListenFor(MainRelease)(scope);
    var end = Timeval.TickCount;
    var ticks = Mathf.Min(max, end-start);
    Debug.Log($"End charging. Squeak for {ticks} ticks");
    for (var i = 0; i < ticks; i++) {
      await scope.Any(
        async s => await s.Tick(),
        async s => {
          await ListenFor(Squeak)(s);
          Debug.Log("Squeak!");
        }
      );
    }
    Debug.Log("Ability Complete");
  }

  public override Task MainRelease(TaskScope scope) => null;
  public Task Squeak(TaskScope scope) => null;
}