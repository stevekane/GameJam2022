using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
  public static async Task Await(TaskScope scope, AsyncOperation op) {
    await scope.ListenFor(new AsyncOperationEventSource(op));
  }

  public static GameManager Instance;

  [Header("Subsystems")]
  public Defaults Defaults;
  public SFXManager SFXManager;
  public VFXManager VFXManager;
  public ProjectileManager ProjectileManager;
  public MobManager MobManager;
  public GrapplePointManager GrapplePointManager;
  public CameraManager CameraManager;

  [Header("Player")]
  public GameObject PlayerPrefab;
  public Transform PlayerSpawn;

  public TaskScope GlobalScope = new();

  Player Player;

  void Awake() {
    Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
    if (Instance) {
      Destroy(gameObject);
    } else {
      Instance = this;
      Defaults.Instance = Defaults;
      SFXManager.Instance = SFXManager;
      VFXManager.Instance = VFXManager;
      ProjectileManager.Instance = ProjectileManager;
      MobManager.Instance = MobManager;
      GrapplePointManager.Instance = GrapplePointManager;
      CameraManager.Instance = CameraManager;
      this.InitComponentFromChildren(out DebugUI.Instance, true);
      this.InitComponentFromChildren(out NavMeshUtil.Instance);
      this.InitComponentFromChildren(out PlayerManager.Instance);
      DontDestroyOnLoad(Instance.gameObject);
    }
  }

  void Start() {
    GlobalScope.Start(Run);
  }

  void OnDestroy() {
    GlobalScope.Cancel();
  }

  void FixedUpdate() {
    Timeval.TickCount++;
    Timeval.TickEvent.Fire();
  }

  TaskFunc EncounterDefeated(Encounter encounter) => async (TaskScope scope) => {
    await encounter.Run(scope);
  };

  TaskFunc PlayerDeath(Player player) => async (TaskScope scope) => {
    await scope.Until(() => player.Dead);
  };

  async Task Run(TaskScope scope) {
    while (true) {
      await scope.Any(WaitForInput, GameLoop);
      await scope.Tick();
    }
  }

  static KeyCode[] StartEncounterKeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
  async Task WaitForInput(TaskScope scope) {
    while (true) {
      if (Input.GetKeyDown(KeyCode.Backspace)) {
        await ReloadScene(scope);
        return;
      } else if (Array.FindIndex(StartEncounterKeys, k => Input.GetKeyDown(k)) is var idx && idx >= 0) {
        await ReloadScene(scope);
        StartEncounter(idx);
        return;
      }
      await scope.Tick();
    }
  }

  async Task GameLoop(TaskScope scope) {
    // Spawn and configure the player
    Player = FindObjectOfType<Player>();
    Player = Player ?? SpawnPlayer();
    SaveData.LoadFromFile();
    DebugUI.Log(this, $"looping");
    // Setup camera to target the player
    //PlayerVirtualCamera.Instance.Follow = Player.transform;

    // TODO: Eliminate this hack to allow loaded upgrades to apply before opening the shop
    await scope.Ticks(2);
    // Wait for the player to purchase upgrades
    if (FindObjectOfType<Shop>() is var shop && shop != null) {
      shop.Open();
      await scope.Until(() => !shop.IsOpen);
    }

    // Begin encounter
    var encounter = await FindEncounter(scope);
    var encounterDefeated = EncounterDefeated(encounter);
    var playerDeath = PlayerDeath(Player);
    var outcome = await scope.Any(encounterDefeated, playerDeath);
    Debug.Log(outcome switch {
      0 => "You win",
      _ => "You lose"
    });

    SaveData.SaveToFile();

    Destroy(Player.gameObject);
    await scope.Millis(3000);

    await ReloadScene(scope);
  }

  void StartEncounter(int index) {
    var encounters = FindObjectsOfType<Encounter>(true);
    encounters.ForEach((e, i) => e.gameObject.SetActive(i == index));
  }

  async Task<Encounter> FindEncounter(TaskScope scope) {
    while (true) {
      if (FindObjectOfType<Encounter>() is var encounter && encounter != null)
        return encounter;
      await scope.Millis(1000);
    }
  }

  async Task ReloadScene(TaskScope scope) {
    await Await(scope, SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name));
  }

  public Player SpawnPlayer() {
    return Instantiate(PlayerPrefab, PlayerSpawn.position, PlayerSpawn.rotation).GetComponent<Player>();
  }

  void SetPlayerInputsEnabled(GameObject player, bool isEnabled) {
    player.GetComponent<InputToTriggerMap>().enabled = isEnabled;
  }

  bool GameOver() {
    return !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0f) <= 0;
  }
}