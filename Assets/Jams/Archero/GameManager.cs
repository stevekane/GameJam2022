using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archero {
  public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    public List<SceneAsset> Scenes;
    public int CurrentLevel => Scenes.FindIndex(s => s.name == SceneManager.GetActiveScene().name);

    public List<Upgrade> Upgrades;

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
        DontDestroyOnLoad(Instance.gameObject);
      }
    }

    void FixedUpdate() {
      Timeval.TickCount++;
      Timeval.TickEvent.Fire();
    }

    public void OnLevelComplete() {
      GlobalScope.Start(async s => {
        await s.While(() => CollectingCoins);
        if (CurrentLevel+1 < Scenes.Count) {
          var next = Scenes[CurrentLevel+1];
          SceneManager.LoadSceneAsync(next.name);
        } else {
          Debug.Log($"Victory!");
        }
      });
    }

    bool CollectingCoins = false;
    public void OnMobsCleared() {
      GlobalScope.Start(async s => {
        CollectingCoins = true;
        FindObjectOfType<Door>().Open();
        await s.Seconds(.5f);
        var coins = FindObjectsOfType<Coin>();
        var tasks = coins.Select(c => c.Collect(s));
        await s.AllTask(tasks.ToArray());
        Player.Instance.GetComponent<Upgrades>().MaybeLevelUp();
        CollectingCoins = false;
      });
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