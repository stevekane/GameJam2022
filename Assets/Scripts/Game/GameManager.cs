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

  IEnumerator EncounterDefeated(Encounter encounter) {
    yield return new Fiber(encounter.Run());
    yield return Fiber.Until(() => MobManager.Instance.Mobs.Count <= 0);
  }

  IEnumerator PlayerDeath(GameObject player) {
    yield return Fiber.Until(() => player == null);
  }

  IEnumerator Run() {
    while (true) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      var p = playerSpawn.transform.position;
      var r = playerSpawn.transform.rotation;
      Player = Instantiate(PlayerPrefab, p, r);
      SaveData.LoadFromFile();

      // Setup camera to target the player
      PlayerVirtualCamera.Instance.Follow = Player.transform;

      // TODO: Eliminate this hack to allow loaded upgrades to apply before opening the shop
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

      // Begin GameLoop
      Encounter.Bundle = Bundle;
      var encounterDefeated = EncounterDefeated(Encounter);
      var playerDeath = PlayerDeath(Player);
      var outcome = Fiber.Select(encounterDefeated, playerDeath);
      // TODO: What if both happen at the same time? stupid concurrency
      yield return outcome;
      // TODO: Returning an int is pretty horrible... maybe just return the completed Fiber
      Debug.Log(outcome.Value switch {
        0 => "You win",
        _ => "You lose"
      });
      InputManager.Instance.SetInputEnabled(false);
      yield return Fiber.Wait(Timeval.FramesPerSecond * 3);
      // End GameLoop

      // Cleanup references and reload the scene
      yield return Await(SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
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

  IEnumerator Countdown(
  Action<int> f,
  int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      yield return Fiber.Wait(Timeval.FramesPerSecond);
    }
  }

}