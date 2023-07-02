using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archero {
  public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    public Bounds VictoryBounds;

    public List<SceneAsset> Scenes;
    public int CurrentRoom => Scenes.FindIndex(s => s.name == SceneManager.GetActiveScene().name);

    public List<Upgrade> Upgrades;

    public Heart HeartPrefab;
    public Coin CoinPrefab;
    public Circle CirclePrefab;
    public Bolt BoltPrefab;
    public GameObject AuraPrefab;

    public TaskScope GlobalScope = new();

    void Awake() {
      Time.fixedDeltaTime = 1f / Timeval.FixedUpdatePerSecond;
      if (Instance) {
        Destroy(gameObject);
      } else {
        Instance = this;
        this.InitComponentFromChildren(out UpgradeUI.Instance);
        this.InitComponentFromChildren(out MessageUI.Instance);
        transform.SetParent(null);
        DontDestroyOnLoad(Instance.gameObject);

        GlobalScope.Start(GameMain);
      }
    }
    void OnDestroy() => GlobalScope.Dispose();

    async Task GameMain(TaskScope scope) {
      await scope.Ticks(2);  // ensure player and upgradeUI have initted
      Player.Instance.GetComponent<Upgrades>().ChooseFirstUpgrade();
    }

    void FixedUpdate() {
      Timeval.TickCount++;
      Timeval.TickEvent.Fire();
    }

    public void RestartGame() {
      if (Player.Instance)
        Player.Instance.gameObject.Destroy();
      SceneManager.LoadSceneAsync(Scenes[0].name);
      Destroy(gameObject);
    }

    bool CollectingDrops = false;
    void OnMobsCleared() {
      GlobalScope.Start(async scope => {
        CollectingDrops = true;
        FindObjectOfType<RoomEvents>().OnRoomClear.Invoke();
        FindObjectOfType<Door>().Open();
        await scope.Seconds(.5f);
        var tasks =
            FindObjectsOfType<Coin>().Select(c => c.Collect(scope))
            .Concat(FindObjectsOfType<Heart>().Select(h => h.Collect(scope)));
        await scope.AllTask(tasks.ToArray());
        await scope.Seconds(.5f);
        do {
          await scope.While(() => UpgradeUI.Instance.IsShowing);
          Player.Instance.GetComponent<Upgrades>().MaybeLevelUp();
        } while (UpgradeUI.Instance.IsShowing);
        CollectingDrops = false;
      });
    }

    public void OnRoomExited() {
      GlobalScope.Start(async scope => {
        await scope.While(() => CollectingDrops);
        if (CurrentRoom+1 < Scenes.Count) {
          var next = Scenes[CurrentRoom+1];
          SceneManager.LoadSceneAsync(next.name);
        } else {
          Player.Instance.GetComponent<Status>().Add(new InlineEffect(s => {
            s.CanMove = false;
            s.CanRotate = false;
            s.CanAttack = false;
          }, "GameOver"));
          for (var i = 0; i < 10; i++) {
            var xz = new Vector3(UnityEngine.Random.Range(VictoryBounds.min.x, VictoryBounds.max.x), 0f, UnityEngine.Random.Range(VictoryBounds.min.z, VictoryBounds.max.z));
            Coin.SpawnCoins(xz, UnityEngine.Random.Range(10, 30));
            await scope.Seconds(UnityEngine.Random.Range(.1f, 1f));
          }
          MessageUI.Instance.Show("Victory!\n\nPress X to play again.", "Restart");
          await scope.While(() => MessageUI.Instance.IsShowing);
          RestartGame();
        }
      });
    }

    public void OnPlayerDied() {
      GlobalScope.Start(async scope => {
        await scope.Seconds(1f);
        MessageUI.Instance.Show("You died!\n\nPress X to restart.", "Restart");
        await scope.While(() => MessageUI.Instance.IsShowing);
        RestartGame();
      });
    }

    public void OnMobDied() {
      Invoke("CheckMobsCleared", .05f);  // Gives a chance for DeathSplit mobs to spawn.
    }

    void CheckMobsCleared() {
      if (MobManager.Instance.Mobs.Count == 0)
        OnMobsCleared();
    }

    [ContextMenu("Spawn coins")]
    void SpawnCoins() {
      var xz = UnityEngine.Random.insideUnitCircle * 3;
      Coin.SpawnCoins(new(xz.x, 0f, xz.y), 100);
      Invoke("OnMobsCleared", 2f);
    }

    [ContextMenu("LevelUp")]
    void LevelUp() {
      var ups = Player.Instance.GetComponent<Upgrades>();
      ups.XP += ups.XPToNextLevel;
      ups.MaybeLevelUp();
    }
  }
}