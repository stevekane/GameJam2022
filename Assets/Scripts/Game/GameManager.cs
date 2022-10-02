using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour {
  public static IEnumerator Await(AsyncOperation op) {
    while (!op.isDone) {
      yield return op;
    }
  }

  public static GameManager Instance;

  [Header("Countdown")]
  public AudioClip[] CountdownClips;
  public int CountdownDuration = 3;
  public TextMeshProUGUI CountdownText;

  [Header("Player")]
  public GameObject PlayerPrefab;
  public List<Spawn> PlayerSpawns = new();

  [Header("Mobs")]
  public GameObject PreviewPrefab;
  public GameObject SpawnEffectPrefab;
  public GameObject WaspPrefab;
  public GameObject BadgerPrefab;
  public Transform[] MobSpawns;

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
    SFXManager.Instance.TryPlayOneShot(CountdownClips[n%CountdownClips.Length]);
    CountdownText.text = n.ToString();
  }

  void SetCountdownTextEnabled(TextMeshProUGUI text, bool isEnabled) {
    text.enabled = isEnabled;
  }

  void SetPlayerInputsEnabled(GameObject player, bool isEnabled) {
    player.GetComponent<InputToTriggerMap>().enabled = isEnabled;
  }

  // N.B. Approximate as you cannot guarantee you will be called back in exactly 1 second
  IEnumerator Countdown(Action<int> f, int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      yield return new WaitForSeconds(1);
    }
  }

  IEnumerator SpawnMob(
  GameObject previewPrefab,
  GameObject spawnEffectPrefab,
  GameObject mobPrefab,
  float previewDuration,
  Transform targetTransform) {
    var p = Instantiate(previewPrefab, targetTransform.position, targetTransform.rotation);
    yield return new WaitForSeconds(previewDuration);
    Destroy(p.gameObject);
    VFXManager.Instance.TrySpawnEffect(spawnEffectPrefab, targetTransform.position);
    var m = Instantiate(mobPrefab, targetTransform.position, targetTransform.rotation);
  }

  bool GameOver() => !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health) <= 0;

  IEnumerator Start() {
    while (true) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      Player = Instantiate(PlayerPrefab, playerSpawn.transform.position, playerSpawn.transform.rotation);

      // Enter pre-game countdown
      SetPlayerInputsEnabled(Player, isEnabled: false);
      SetCountdownTextEnabled(CountdownText, isEnabled: true);
      yield return StartCoroutine(Countdown(PingCountdown, CountdownDuration));
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
      SetPlayerInputsEnabled(Player, isEnabled: true);
      // Exit pre-game countdown

      var i = 0;
      while (!GameOver()) {
        yield return StartCoroutine(SpawnMob(
          previewPrefab: PreviewPrefab,
          spawnEffectPrefab: SpawnEffectPrefab,
          mobPrefab: BadgerPrefab,
          previewDuration: 3,
          targetTransform: MobSpawns[i]
        ));
        i = (i+1)%MobSpawns.Length;
      }

      // Cleanup references and reload the scene
      yield return StartCoroutine(Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name)));
    }
  }
}