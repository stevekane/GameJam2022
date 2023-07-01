using UnityEngine;

namespace Archero {
  public class Mob : MonoBehaviour {
    int Coins => 10 + 2*GameManager.Instance.CurrentRoom;

    public void DropCoins() {
      var amount = (int)(Player.Instance.GetComponent<Attributes>().GetValue(AttributeTag.GoldGain, Coins * Random.Range(.75f, 1.5f)));
      Coin.SpawnCoins(transform.position.XZ(), amount);
      Heart.MaybeSpawn(transform.position.XZ());
    }

    void OnDeath() {
      DropCoins();
      MobManager.Instance?.Mobs.Remove(this);
      GameManager.Instance.OnMobDied();
      Destroy(gameObject, .01f);
    }

    void Awake() {
      MobManager.Instance?.Mobs.Add(this);
      Debug.Log("Mob awake");
    }

    void OnDestroy() {
      MobManager.Instance?.Mobs.Remove(this);
    }
  }
}