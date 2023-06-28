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

    public List<SceneAsset> Scenes;
    public int CurrentLevel => Scenes.FindIndex(s => s.name == SceneManager.GetActiveScene().name);

    public List<Upgrade> Upgrades;

    public Coin CoinPrefab;

    TaskScope GlobalScope = new();

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
      if (CurrentLevel+1 < Scenes.Count) {
        var next = Scenes[CurrentLevel+1];
        SceneManager.LoadSceneAsync(next.name);
      } else {
        Debug.Log($"Victory!");
      }
    }

    public void OnMobsCleared() {
      GlobalScope.Start(async s => {
        FindObjectOfType<Door>().Open();
        await s.Seconds(.5f);
        var coins = FindObjectsOfType<Coin>();
        var tasks = coins.Select(c => c.Collect(s));
        await s.AllTask(tasks.ToArray());
        Player.Get().GetComponent<Upgrades>().MaybeLevelUp();
        Debug.Log($"Got em all");
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
      var ups = Player.Get().GetComponent<Upgrades>();
      ups.XP += ups.XPToNextLevel;
      ups.MaybeLevelUp();
    }
  }
}