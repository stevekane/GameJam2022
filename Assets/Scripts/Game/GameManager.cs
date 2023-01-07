using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour {
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

  [Header("Player")]
  public GameObject PlayerPrefab;
  public Transform PlayerSpawn;

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
      Player = Instantiate(PlayerPrefab, PlayerSpawn.position, PlayerSpawn.rotation).GetComponent<Player>();
      SaveData.LoadFromFile();

      // Setup camera to target the player
      //PlayerVirtualCamera.Instance.Follow = Player.transform;

      // TODO: Eliminate this hack to allow loaded upgrades to apply before opening the shop
      await scope.Ticks(2);
      // Wait for the player to purchase upgrades
      var shop = FindObjectOfType<Shop>();
      if (shop != null) {
        shop.Open();
        await scope.Until(() => !shop.IsOpen);
      }

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

  void SetPlayerInputsEnabled(GameObject player, bool isEnabled) {
    player.GetComponent<InputToTriggerMap>().enabled = isEnabled;
  }

  bool GameOver() {
    return !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0f) <= 0;
  }
}