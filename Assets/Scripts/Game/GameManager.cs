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
      this.InitComponentFromChildren(out WorkerManager.Instance);
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
  }

  TaskFunc EncounterDefeated(Encounter encounter) => async (TaskScope scope) => {
    await encounter.Run(scope);
  };

  TaskFunc PlayerDeath(Player player) => async (TaskScope scope) => {
    await scope.Until(() => player.Dead);
  };

  async Task Run(TaskScope scope) {
    while (true) {
      await GameLoop(scope);
      await scope.Tick();
    }
  }

  static KeyCode[] StartEncounterKeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
  async Task HandleSceneLoad(TaskScope scope) {
    if (Input.GetKeyDown(KeyCode.Backspace)) {
      await ReloadScene(scope);
      return;
    } else if (Array.FindIndex(StartEncounterKeys, k => Input.GetKeyDown(k)) is var idx && idx >= 0) {
      await ReloadScene(scope);
      StartEncounter(idx);
      return;
    }
  }

  // Hacky save/load. Entirely the wrong approach, but good enough for now.
  static KeyCode[] SaveLoadKeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, };
  void HandleSaveLoad() {
    for (int i = 0; i < SaveLoadKeys.Length; i++) {
      if (Input.GetKeyDown(SaveLoadKeys[i])) {
        if (Input.GetKey(KeyCode.LeftAlt)) {
          SaveData.SaveToFile(i);
        } else {
          SaveData.LoadFromFile(i);
        }
      }
    }
  }

  void Update() {
    HandleSaveLoad();
  }

  async Task GameLoop(TaskScope scope) {
    // Spawn and configure the player
    Player = FindObjectOfType<Player>();
    Player = Player ?? SpawnPlayer();
    if (!Player)  // No player prefab means don't run a game loop
      await scope.Forever();
    SaveData.LoadFromFile(0);
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

    SaveData.SaveToFile(0);

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
    if (PlayerPrefab)
      return Instantiate(PlayerPrefab, PlayerSpawn.position, PlayerSpawn.rotation).GetComponent<Player>();
    return null;
  }

  void SetPlayerInputsEnabled(GameObject player, bool isEnabled) {
    player.GetComponent<InputToTriggerMap>().enabled = isEnabled;
  }

  bool GameOver() {
    return !Player || Player.GetComponent<Attributes>().GetValue(AttributeTag.Health, 0f) <= 0;
  }
}