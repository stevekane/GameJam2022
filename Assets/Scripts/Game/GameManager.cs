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
  public TextMeshProUGUI CountdownText;
  public int CountdownDuration = 3;

  [Header("Player")]
  public GameObject PlayerPrefab;
  public List<Spawn> PlayerSpawns = new();

  [Header("Mobs")]
  public Encounter Encounter;

  Bundle Bundle = new Bundle();
  GameObject Player;
  List<GameObject> Mobs;

  void Awake() {
    if (Instance) {
      Instance.Bundle.StopAll();
      Destroy(gameObject);
    } else {
      Instance = this;
      DontDestroyOnLoad(Instance.gameObject);
    }
  }

  void Start() {
    Bundle.StartRoutine(new Fiber(Run()));
  }

  void FixedUpdate() {
    Bundle.Run();
  }

  IEnumerator Run() {
    //while (true) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      var p = playerSpawn.transform.position;
      var r = playerSpawn.transform.rotation;
      Player = Instantiate(PlayerPrefab, p, r);
      SaveData.LoadFromFile();

      // Setup camera to target the player
      PlayerVirtualCamera.Instance.Follow = Player.transform;

      // Cheap hack to allow loaded upgrades to apply before opening the shop
      yield return Fiber.Wait(2);
      // Wait for the player to purchase upgrades
      var shop = GetComponent<Shop>();
      shop.Open();
      yield return Fiber.Until(() => !shop.IsOpen);

      // Enter pre-game countdown
      InputManager.Instance.SetInputEnabled(false);
      SetCountdownTextEnabled(CountdownText, isEnabled: true);
      yield return Countdown(PingCountdown, CountdownDuration);
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
      InputManager.Instance.SetInputEnabled(true);
      // Exit pre-game countdown

      // Begin Encounter
      Encounter.Bundle = Bundle;
      yield return Encounter.Run();
      // End Encounter

      Debug.Log("Beyond the encounter");

      // Cleanup references and reload the scene
      // yield return Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
    // }
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

  IEnumerator Countdown(
  Action<int> f,
  int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      yield return Fiber.Wait(Timeval.FramesPerSecond);
    }
  }

}