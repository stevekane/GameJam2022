using System.Collections.Generic;
using System.Threading.Tasks;

public class WaveEncounter : Encounter {
  public List<SpawnGroup> Waves;

  public override async Task Run(TaskScope scope) {
    for (int wave = 0; wave < Waves.Count; wave++) {
      var spawners = Waves[wave];
      await spawners.Spawn(scope, wave);
      await scope.Until(() => MobManager.Instance.Mobs.Count <= 0);
    }
  }
}