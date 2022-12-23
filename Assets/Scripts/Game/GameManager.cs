using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour {
  public static IEnumerator Await(AsyncOperation op) {
    yield return Fiber.ListenFor(new AsyncOperationEventSource(op));
  }
  public static async Task Await(TaskScope scope, AsyncOperation op) {
    await scope.ListenFor(new AsyncOperationEventSource(op));
  }

  public static GameManager Instance;

  // Subsystems now depend on this class, so we need it. But not every scene wants a game loop.
  // Would probably be better to disentangle the game loop from the game singleton.
  public bool ManageGameLoop = true;

  [Header("Subsystems")]
  public Defaults Defaults;
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

  public TaskScope GlobalScope = new();

  Player Player;

  void Awake() {
    if (Instance) {
      Destroy(gameObject);
    } else {
      Instance = this;
      Defaults.Instance = Defaults;
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
    GlobalScope.Start(Run);
  }

  void OnDestroy() {
    GlobalScope.Cancel();
  }

  TaskFunc EncounterDefeated(Encounter encounter) => async (TaskScope scope) => {
    await encounter.Run(scope);
    await scope.Until(() => MobManager.Instance.Mobs.Count <= 0);
  };

  TaskFunc PlayerDeath(Player player) => async (TaskScope scope) => {
    await scope.Until(() => player.Dead);
  };

  async Task Run(TaskScope scope) {
    while (!ManageGameLoop) {
      if (!ManageGameLoop && Input.GetKeyDown(KeyCode.Backspace)) {
        await Await(scope, SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
      } else {
        await scope.Tick();
      }
    }
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
      await scope.Ticks(2);
      // Wait for the player to purchase upgrades
      var shop = FindObjectOfType<Shop>();
      shop.Open();
      await scope.Until(() => !shop.IsOpen);

      // Enter pre-game countdown
      InputManager.Instance.SetInputEnabled(false);
      SetCountdownTextEnabled(CountdownText, isEnabled: true);
      await Countdown(scope, PingCountdown, CountdownDuration);
      SetCountdownTextEnabled(CountdownText, isEnabled: false);
      InputManager.Instance.SetInputEnabled(true);
      // Exit pre-game countdown

      // Begin GameLoop
      var encounter = FindObjectOfType<Encounter>();
      var encounterDefeated = EncounterDefeated(encounter);
      var playerDeath = PlayerDeath(Player);
      var outcome = await scope.Any(encounterDefeated, playerDeath);
      Debug.Log(outcome switch {
        0 => "You win",
        _ => "You lose"
      });
      SaveData.SaveToFile();
      Destroy(Player.gameObject);
      InputManager.Instance.SetInputEnabled(false);
      await scope.Millis(3000);
      // End GameLoop

      // Cleanup references and reload the scene
      await Await(scope, SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
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

  async Task Countdown(TaskScope scope, Action<int> f, int seconds) {
    for (var i = seconds; i >= 0; i--) {
      f(i);
      await scope.Millis(1000);
    }
  }

}