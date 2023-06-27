using UnityEngine;

namespace Archero {
  public class Mob : MonoBehaviour {
    int Coins => 50 + 5*GameManager.Instance.CurrentLevel;

    public void DropCoins() {
      var amount = (int)(Player.Get().GetComponent<Attributes>().GetValue(AttributeTag.GoldGain, Coins * Random.Range(.75f, 1.5f)));
      Coin.SpawnCoins(transform.position.XZ(), amount);
    }

    void OnDeath() {
      DropCoins();
    }

    void Start() {
      MobManager.Instance?.Mobs.Add(this);
    }

    void OnDestroy() {
      MobManager.Instance?.Mobs.Remove(this);
      if (MobManager.Instance?.Mobs.Count == 0)
        GameManager.Instance.OnMobsCleared();
    }
  }
}