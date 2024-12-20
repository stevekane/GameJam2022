using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WaveEncounter : Encounter {
  public List<SpawnWave> Waves;

  public override async Task Run(TaskScope scope) {
    for (int wave = 0; wave < Waves.Count; wave++) {
      var spawners = Waves[wave];
      await spawners.Spawn(scope, wave);
      await scope.Ticks(100);
      await scope.Until(() => MobManager.Instance.Mobs.Count <= 0);
    }
  }
}