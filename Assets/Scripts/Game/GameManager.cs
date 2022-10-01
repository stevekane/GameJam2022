using System;
using System.Collections;
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

  public GameObject PlayerPrefab;
  public TextMeshProUGUI CountdownText;
  public AudioClip[] CountdownClips;

  public int CountdownDuration = 3;

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

  IEnumerator Start() {
    while (true) {
      // Spawn and configure the player
      var playerSpawns = FindObjectsOfType<PlayerSpawn>();
      var playerSpawn = playerSpawns[0];
      Player = Instantiate(PlayerPrefab, playerSpawn.transform.position, playerSpawn.transform.rotation);
      // Enter pre-game countdown
      SetPlayerInputsEnabled(Player, isEnabled: false);
      SetCountdownTextEnabled(CountdownText, isEnabled: true);
      yield return StartCoroutine(Countdown(PingCountdown, CountdownDuration));
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
      SetPlayerInputsEnabled(Player, isEnabled: true);
      // Exit pre-game countdown
      // Spawn mobs
      var mobSpawns = FindObjectsOfType<MobSpawn>();
      // Wait for gameover condition to be met
      yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
      // Cleanup references and reload the scene
      Player = null;
      yield return StartCoroutine(Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name)));
    }
  }
}