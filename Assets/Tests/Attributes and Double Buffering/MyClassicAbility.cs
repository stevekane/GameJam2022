using System.Threading.Tasks;
using UnityEngine;

public class MyClassicAbility : Ability {
  bool Squeaking;
  bool Charging;

  public override bool CanStart(AbilityMethod func) {
    return func == MainAction && !IsRunning ||
           func == MainRelease && Charging ||
           func == Squeak && Squeaking;
  }

  public override async Task MainAction(TaskScope scope) {
    try {
      Charging = true;
      var start = Timeval.TickCount;
      var max = 120;
      Debug.Log("Start charging");
      await ListenFor(MainRelease)(scope);
      var end = Timeval.TickCount;
      var ticks = Mathf.Min(max, end-start);
      Charging = false;
      Squeaking = true;
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
    } finally {
      Debug.Log("Ability Complete");
      Charging = false;
      Squeaking = false;
    }
  }

  public override Task MainRelease(TaskScope scope) => null;
  public Task Squeak(TaskScope scope) => null;
}