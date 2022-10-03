using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[Serializable]
public struct Effect {
  public GameObject GameObject;
  public float Duration;
}

[Serializable]
public struct MobSpawnConfig {
  public Effect PreviewEffect;
  public Effect SpawnEffect;
  public GameObject Mob;
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
  public MobSpawnConfig[] MobSpawnConfigs;

  GameObject Player;

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
    return !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health) <= 0;
  }

  IEnumerator Countdown(
  Action<int> f,
  int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      yield return new WaitForSeconds(1);
    }
  }

  IEnumerator SpawnMob(MobSpawnConfig config, Transform targetTransform) {
    var p = targetTransform.position;
    var r = targetTransform.rotation;
    VFXManager.Instance.SpawnEffect(config.PreviewEffect, p, r);
    yield return new WaitForSeconds(config.PreviewEffect.Duration);
    VFXManager.Instance.SpawnEffect(config.SpawnEffect, p, r);
    Instantiate(config.Mob, p, r);
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

      var i = 0;
      var j = 0;
      while (!GameOver()) {
        yield return StartCoroutine(SpawnMob(MobSpawnConfigs[j], MobSpawns[i]));
        i = (i+1)%MobSpawns.Length;
        j = (j+1)%MobSpawnConfigs.Length;
      }

      // Cleanup references and reload the scene
      yield return StartCoroutine(Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name)));
    }
  }
}