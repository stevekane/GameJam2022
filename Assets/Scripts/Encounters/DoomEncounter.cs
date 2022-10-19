using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoomEncounter : Encounter {
  public SpawnConfig HeavySpawnConfig;
  public SpawnConfig LightSpawnConfig;
  public List<Transform> HeavySpawnPoints;
  public List<Transform> LightSpawnPoints;

  class MobSpawner {
    public int TotalSpawnCount;
    public int SpawningCount;
    public List<GameObject> Mobs = new();
    public int ActiveCount { get => SpawningCount+Mobs.Count; }
  }

  IEnumerator SpawnMob(MobSpawner spawner, SpawnRequest sr) {
    var p = sr.transform.position;
    var r = sr.transform.rotation;
    spawner.SpawningCount++;
    VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
    yield return new WaitForSeconds(sr.config.PreviewEffect.Duration.Frames);
    VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
    spawner.SpawningCount--;
    spawner.Mobs.Add(Instantiate(sr.config.Mob, p, r));
  }

  IEnumerator SpawnMobs(MobSpawner spawner, int min, SpawnConfig spawnConfig, List<Transform> spawnPoints) {
    while (true) {
      yield return new WaitUntil(() => spawner.ActiveCount < min);
      for (var i = spawner.Mobs.Count; i < min; i++) {
        var spawnPoint = spawnPoints[i%spawnPoints.Count];
        var spawnRequest = new SpawnRequest { config = spawnConfig, transform = spawnPoint };
        StartCoroutine(SpawnMob(spawner, spawnRequest));
      }
    }
  }

  IEnumerator RemoveDeadMobs(List<GameObject> mobs) {
    while (true) {
      mobs.RemoveAll(m => m == null);
      yield return null;
    }
  }

  IEnumerator EncounterDone() {
    while (true) {
      yield return null;
    }
  }

  public override IEnumerator Run() {
    var lightSpawner = new MobSpawner();
    var heavySpawner = new MobSpawner();
    var spawnLightMobs = StartCoroutine(SpawnMobs(lightSpawner, 3, LightSpawnConfig, LightSpawnPoints));
    var spawnHeavyMobs = StartCoroutine(SpawnMobs(heavySpawner, 1, HeavySpawnConfig, HeavySpawnPoints));
    var removeDeadLightMobs = StartCoroutine(RemoveDeadMobs(lightSpawner.Mobs));
    var removeDeadHeavyMobs = StartCoroutine(RemoveDeadMobs(heavySpawner.Mobs));
    yield return EncounterDone();
  }
}