using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
  public int InitialCost;
  public int CostPerWave;
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
    var price = 0;
    foreach (var t in transforms) {
      foreach (var c in candidates) {
        var nextPrice = Cost(config.Costs, c.Mob)+price;
        if (nextPrice <= budget) {
          price = nextPrice;
          yield return new SpawnRequest { config = c, transform = t };
          break;
        }
      }
    }
  }

  IEnumerator Spawn(SpawnRequest sr, ISpawnAction<GameObject> spawnAction = null) {
    var p = sr.transform.position;
    var r = sr.transform.rotation;
    VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
    yield return Fiber.Wait(sr.config.PreviewEffect.Duration.Frames);
    VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
    spawnAction?.OnSpawn(Instantiate(sr.config.Mob, p, r));
  }

  public override IEnumerator Run() {
    for (var waveNumber = 0; waveNumber < TotalWaveCount; waveNumber++) {
      yield return Fiber.Wait(WavePeriod.Frames);
      var spawnAction = new MobSpawnAction { WaveNumber = waveNumber };
      foreach (var wave in Wave(InitialCost+CostPerWave*waveNumber, Config)) {
        var spawn = new Fiber(Spawn(wave, spawnAction));
        Bundle.StartRoutine(spawn);
      }
    }
  }
}