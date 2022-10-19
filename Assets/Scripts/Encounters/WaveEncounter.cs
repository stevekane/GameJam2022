using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MobStrategy { Base }
public enum SpawnStrategy { Base }
public enum SpawnProcess { Staggered, Concurrent } // TODO: needs parameters for wave size and spawn period
public enum NextWaveCondition { Time } // TODO: needs paramater for duration and other params

[Serializable]
public struct Wave {
  public int Budget;
  public MobStrategy MobStrategy;
  public SpawnStrategy SpawnStrategy;
  public SpawnProcess SpawnProcess;
  public NextWaveCondition NextWaveCondition;
}

public class WaveEncounter : Encounter {
  public List<SpawnConfig> SpawnConfigs;
  public List<Transform> SpawnPoints;
  public List<Wave> Waves;

  IEnumerator SpawnMob(SpawnRequest sr) {
    var p = sr.transform.position;
    var r = sr.transform.rotation;
    VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
    yield return new WaitForSeconds(sr.config.PreviewEffect.Duration.Seconds);
    VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
    Instantiate(sr.config.Mob, p, r);
  }

  IEnumerator SpawnConcurrent(List<SpawnRequest> spawnRequests) {
    foreach (var sr in spawnRequests) {
      StartCoroutine(SpawnMob(sr));
    }
    yield return null;
  }

  IEnumerator SpawnStaggered(List<SpawnRequest> spawnRequests, int groupSize, float seconds) {
    var batch = new List<SpawnRequest>();
    for (var i = 0; i < spawnRequests.Count; i++) {
      batch.Add(spawnRequests[i]);
      if (batch.Count == groupSize) {
        StartCoroutine(SpawnConcurrent(batch));
        batch.Clear();
        yield return new WaitForSeconds(seconds);
      }
    }
  }

  List<SpawnConfig> BaseMobs(List<SpawnConfig> spawnConfigs) {
    return spawnConfigs;
  }

  List<SpawnRequest> BaseSpawns(List<SpawnConfig> spawnConfigs, List<Transform> spawnPoints) {
    var spawnRequests = new List<SpawnRequest>(spawnConfigs.Count);
    for (var i = 0; i < spawnConfigs.Count; i++) {
      spawnRequests.Add(new SpawnRequest {
        config = spawnConfigs[i],
        transform = spawnPoints[i%spawnPoints.Count]
      });
    }
    return spawnRequests;
  }

  public override IEnumerator Run() {
    foreach (var wave in Waves) {
      var mobs = wave.MobStrategy switch {
        _ => BaseMobs(SpawnConfigs)
      };
      var spawns = wave.SpawnStrategy switch {
        _ => BaseSpawns(mobs, SpawnPoints)
      };
      yield return StartCoroutine(wave.SpawnProcess switch {
        SpawnProcess.Staggered => SpawnStaggered(spawns, 1, 2), // TODO: hardcoded.. need to decide where these params live
        SpawnProcess.Concurrent => SpawnConcurrent(spawns)
      });
      yield return wave.NextWaveCondition switch {
        _ => new WaitForSeconds(5) // TODO: hardcoded.. need to decide where this parameter lives
      };
    }
  }
}