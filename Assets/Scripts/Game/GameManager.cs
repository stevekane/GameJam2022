using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/*
An encounter is simply a program with various continuations.

The current program is to spawn a mob every so often at some spawning location.

The program we want now has a few parts:

  Spawning happens in waves
    Concurrent
    Staggered

  Each wave has a continuation condition
    All defeated
    Time elapsed
    Total mobs remaining

  A wave is generated from a seed, a budget, unit prices, mob strategy and spawning strategy

  MobStrategy is an algorithm for consuming the budget
    Expensive units
    Cheap units
    Alternation between expensive and cheap units
    Same unit
    Unique units

  SpawnStrategy is an algorithm for selecting spawn locations for a list of mobs
    Close together
    Far apart
    Evenly spaced
    Random
    Single Spawn

  Seed : Int
  Budget : Int
  Cost : Int
  SpawnOption : (Unit,Cost)
  MobStrategy : Set<SpawnOption> -> [Unit]
  SpawnStrategy : [Unit] -> Set<Transform> -> [(Unit,Transform)]

  A spawning process may
    send spawn requests to the MobManager
    play vfx/sfx

  Loop over waves
    calculate mobstrategy to get list of units
    calculate spawnstrategy to get spawnrequests
    play spawn process
    wait for wave condition (initially passage of time)
*/

public enum MobStrategy { Base }
public enum SpawnStrategy { Base }
public enum SpawnProcess { Staggered } // TODO: needs parameters for wave size and spawn period
public enum NextWaveCondition { Time } // TODO: needs paramater for duration and other params

[Serializable]
public struct Effect {
  public GameObject GameObject;
  public float Duration;
}

[Serializable]
public struct SpawnConfig {
  public int Cost;
  public Effect PreviewEffect;
  public Effect SpawnEffect;
  public GameObject Mob;
}

[Serializable]
public struct SpawnRequest {
  public Transform transform;
  public SpawnConfig config;
}

[Serializable]
public struct Wave {
  public int Budget;
  public MobStrategy MobStrategy;
  public SpawnStrategy SpawnStrategy;
  public SpawnProcess SpawnProcess;
  public NextWaveCondition NextWaveCondition;
}

[Serializable]
public struct Encounter {
  public int Seed;
  public List<SpawnConfig> SpawnConfigs;
  public List<Transform> SpawnPoints;
  public List<Wave> Waves;
}

public class GameManager : MonoBehaviour {
  public static IEnumerator Await(AsyncOperation op) {
    while (!op.isDone) {
      yield return op;
    }
  }

  public static GameManager Instance;

  [Header("Countdown")]
  public AudioClip[] CountdownClips;
  public TextMeshProUGUI CountdownText;
  public int CountdownDuration = 3;

  [Header("Player")]
  public GameObject PlayerPrefab;
  public List<Spawn> PlayerSpawns = new();

  [Header("Mobs")]
  public Transform[] MobSpawns;
  public SpawnConfig[] MobSpawnConfigs;
  public Encounter DefaultEncounter;

  GameObject Player;
  List<GameObject> Mobs;

  void Awake() {
    if (Instance) {
      Destroy(gameObject);
    } else {
      Instance = this;
      DontDestroyOnLoad(Instance.gameObject);
    }
  }

  void PingCountdown(int n) {
    var clip = CountdownClips[n%CountdownClips.Length];
    SFXManager.Instance.TryPlayOneShot(clip);
    CountdownText.text = n.ToString();
  }

  void SetCountdownTextEnabled(TextMeshProUGUI text, bool isEnabled) {
    text.enabled = isEnabled;
  }

  void SetPlayerInputsEnabled(GameObject player, bool isEnabled) {
    player.GetComponent<InputToTriggerMap>().enabled = isEnabled;
  }

  bool GameOver() {
    return !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0f) <= 0;
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

  IEnumerator Countdown(
  Action<int> f,
  int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      yield return new WaitForSeconds(1);
    }
  }

  IEnumerator SpawnMob(SpawnRequest sr) {
    var p = sr.transform.position;
    var r = sr.transform.rotation;
    VFXManager.Instance.SpawnEffect(sr.config.PreviewEffect, p, r);
    yield return new WaitForSeconds(sr.config.PreviewEffect.Duration);
    VFXManager.Instance.SpawnEffect(sr.config.SpawnEffect, p, r);
    Mobs.Add(Instantiate(sr.config.Mob, p, r));
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
        yield return StartCoroutine(SpawnConcurrent(batch));
        yield return new WaitForSeconds(seconds);
        batch.Clear();
      }
    }
  }

  IEnumerator RunEncounter(Encounter encounter) {
    foreach (var wave in encounter.Waves) {
      Debug.Log("Spawning a wave");
      var mobs = wave.MobStrategy switch {
        _ => BaseMobs(encounter.SpawnConfigs)
      };
      var spawns = wave.SpawnStrategy switch {
        _ => BaseSpawns(mobs, encounter.SpawnPoints)
      };
      Debug.Log($"{mobs.Count} mobs spawning at {spawns.Count} locations");
      yield return StartCoroutine(wave.SpawnProcess switch {
        _ => SpawnStaggered(spawns, 1, 2) // TODO: hardcoded.. need to decide where these params live
      });
      yield return wave.NextWaveCondition switch {
        _ => new WaitForSeconds(5) // TODO: hardcoded.. need to decide where this parameter lives
      };
    }
  }

  IEnumerator Start() {
    while (true) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      var p = playerSpawn.transform.position;
      var r = playerSpawn.transform.rotation;
      Player = Instantiate(PlayerPrefab, p, r);

      // Enter pre-game countdown
      SetPlayerInputsEnabled(Player, isEnabled: false);
      SetCountdownTextEnabled(CountdownText, isEnabled: true);
      yield return StartCoroutine(Countdown(PingCountdown, CountdownDuration));
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
      SetPlayerInputsEnabled(Player, isEnabled: true);
      // Exit pre-game countdown

      // Start the encounter
      var encounter = StartCoroutine(RunEncounter(DefaultEncounter));
      while (!GameOver()) {
        yield return null;
      }
      StopCoroutine(encounter);
      // Stop encounter

      // Cleanup references and reload the scene
      yield return StartCoroutine(Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name)));
    }
  }
}