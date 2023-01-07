using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct MobConfig {
  public Mob Mob;
  public int Cost;
  public int MinWave;
}

[Serializable]
public struct WaveConfig {
  public int Budget;
  public int MaxMobs;
}

public class WaveEncounter : Encounter {
  public List<WaveConfig> Waves;
  public List<MobConfig> Mobs;
  int CurrentWave = 0;
  Spawner[] Spawners;
  List<Spawner> FreeSpawners;

  public override async Task Run(TaskScope scope) {
    Spawners = FindObjectsOfType<Spawner>();
    CurrentWave = -1;
    foreach (var wave in Waves) {
      CurrentWave++;
      await DoWave(scope, wave);
    }
  }

  async Task DoWave(TaskScope scope, WaveConfig wave) {
    int remainingBudget = wave.Budget;
    FreeSpawners = Spawners.ToList();
    List<TaskFunc> tasks = new();
    while (ChooseMob(remainingBudget) is var mob && mob.HasValue) {
      Debug.Log($"Wave {CurrentWave}: Spawn mob {mob.Value.Mob} cost is {mob.Value.Cost} / {remainingBudget}");
      Spawner spawner = null;
      do {
        spawner = ChooseSpawner();
        await scope.All(tasks.ToArray());
        tasks.Clear();
        continue;
      } while (spawner == null);

      remainingBudget -= mob.Value.Cost;
      tasks.Add(async s => {
        await spawner.Spawn(s, mob.Value.Mob, CurrentWave);
        FreeSpawners.Add(spawner);
      });
    }
    await scope.All(tasks.ToArray());
    await scope.Until(() => MobManager.Instance.Mobs.Count <= 0);
  }

  Spawner ChooseSpawner() {
    if (FreeSpawners.Count == 0)
      return null;
    var spawner = FreeSpawners[UnityEngine.Random.Range(0, FreeSpawners.Count-1)];
    FreeSpawners.Remove(spawner);
    return spawner;
  }

  MobConfig? ChooseMob(int remainingBudget) {
    var validMobs = Mobs.Where(m => m.Cost <= remainingBudget).ToArray();
    if (validMobs.Length == 0)
      return null;
    return validMobs[UnityEngine.Random.Range(0, validMobs.Length-1)];
  }
}