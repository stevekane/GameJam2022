using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour {
  public class AsyncEvent : IEventSource<AsyncOperation> {
    public AsyncOperation Operation;
    public AsyncEvent(AsyncOperation operation) => Operation = operation;
    public void Listen(Action<AsyncOperation> handler) => Operation.completed += handler;
    public void Unlisten(Action<AsyncOperation> handler) => Operation.completed -= handler;
    public void Fire(AsyncOperation op) {}
  }

  public static IEnumerator Await(AsyncOperation op) {
    yield return Fiber.ListenFor(new AsyncEvent(op));
  }

  public static GameManager Instance;

  // Subsystems now depend on this class, so we need it. But not every scene wants a game loop.
  // Would probably be better to disentangle the game loop from the game singleton.
  public bool ManageGameLoop = true;

  [Header("Subsystems")]
  public InputManager InputManager;
  public SFXManager SFXManager;
  public VFXManager VFXManager;
  public ProjectileManager ProjectileManager;
  public MobManager MobManager;

  [Header("Countdown")]
  public AudioClip[] CountdownClips;
  public TextMeshProUGUI CountdownText;
  public int CountdownDuration = 3;

  [Header("Player")]
  public GameObject PlayerPrefab;
  public List<Spawn> PlayerSpawns = new();

  Bundle Bundle = new Bundle();
  Player Player;

  void Awake() {
    if (Instance) {
      Destroy(gameObject);
    } else {
      Instance = this;
      InputManager.Instance = InputManager;
      SFXManager.Instance = SFXManager;
      VFXManager.Instance = VFXManager;
      ProjectileManager.Instance = ProjectileManager;
      MobManager.Instance = MobManager;
      DontDestroyOnLoad(Instance.gameObject);
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
    }
  }

  void Start() {
    Bundle.StartRoutine(new Fiber(Run()));
  }

  void FixedUpdate() {
    Bundle.MoveNext();
  }

  IEnumerator EncounterDefeated(Encounter encounter) {
    yield return new Fiber(encounter.Run());
    yield return Fiber.Until(() => MobManager.Instance.Mobs.Count <= 0);
  }

  IEnumerator PlayerDeath(Player player) {
    yield return Fiber.Until(() => player.Dead);
  }

  IEnumerator Run() {
    while (ManageGameLoop) {
      // Spawn and configure the player
      var playerSpawn = PlayerSpawns[0];
      var p = playerSpawn.transform.position;
      var r = playerSpawn.transform.rotation;
      Player = Instantiate(PlayerPrefab, p, r).GetComponent<Player>();
      SaveData.LoadFromFile();

      // Setup camera to target the player
      PlayerVirtualCamera.Instance.Follow = Player.transform;

      // TODO: Eliminate this hack to allow loaded upgrades to apply before opening the shop
      yield return Fiber.Wait(2);
      // Wait for the player to purchase upgrades
      var shop = FindObjectOfType<Shop>();
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
      var encounter = FindObjectOfType<Encounter>();
      encounter.Bundle = Bundle;
      var encounterDefeated = EncounterDefeated(encounter);
      var playerDeath = PlayerDeath(Player);
      var outcome = Fiber.Select(encounterDefeated, playerDeath);
      // TODO: What if both happen at the same time? stupid concurrency
      yield return outcome;
      // TODO: Returning an int is pretty horrible... maybe just return the completed Fiber
      Debug.Log(outcome.Value switch {
        0 => "You win",
        _ => "You lose"
      });
      SaveData.SaveToFile();
      Destroy(Player.gameObject);
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