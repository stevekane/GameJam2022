using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public interface ISpawnAction<T> {
  public void OnSpawn(T t);
}

[Serializable]
public struct MobSpawnAction : ISpawnAction<GameObject> {
  public int WaveNumber;
  public void OnSpawn(GameObject go) => go.GetComponent<Mob>().Wave = WaveNumber;
}

[Serializable]
public struct MobCost {
  public GameObject Mob;
  public int Cost;
}

[Serializable]
public struct WaveConfig {
  public Transform[] SpawnPoints;
  public SpawnConfig[] SpawnConfigs;
  public List<MobCost> Costs;
}

public class WaveEncounter : Encounter {
  public int InitialBudget;
  public int BudgetPerWave;
  public int TotalWaveCount;
  public Timeval WavePeriod;
  public WaveConfig Config;

  int Cost(List<MobCost> mobCosts, GameObject mob, int defaultCost = 1) {
    foreach (var mobCost in mobCosts) {
      if (mobCost.Mob == mob) {
        return mobCost.Cost;
      }
    }
    return defaultCost;
  }

  IEnumerable<SpawnRequest> Wave(int budget, WaveConfig config) {
    var candidates = config.SpawnConfigs.OrderByDescending(sc => Cost(config.Costs, sc.Mob));
    var transforms = config.SpawnPoints;
    var remainingBudget = budget;
    foreach (var t in transforms) {
      foreach (var c in candidates) {
        var nextBudget = remainingBudget - Cost(config.Costs, c.Mob);
        if (nextBudget >= 0) {
          remainingBudget = nextBudget;
          yield return new SpawnRequest { config = c, transform = t };
          break;
        }
      }
    }
  }

  TaskFunc Spawn(SpawnRequest sr, ISpawnAction<GameObject> spawnAction = null) => async (TaskScope scope) => {
    var p = sr.transform.position;
    var r = sr.transform.rotation;
    VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
    await scope.Delay(sr.config.PreviewEffect.Duration);
    VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
    spawnAction?.OnSpawn(Instantiate(sr.config.Mob, p, r));
  };

  public override async Task Run(TaskScope scope) {
    for (var waveNumber = 0; waveNumber < TotalWaveCount; waveNumber++) {
      var spawnAction = new MobSpawnAction { WaveNumber = waveNumber };
      foreach (var wave in Wave(InitialBudget+BudgetPerWave*waveNumber, Config)) {
        scope.Start(Spawn(wave, spawnAction));
      }
      await scope.Delay(WavePeriod);
    }
  }
}