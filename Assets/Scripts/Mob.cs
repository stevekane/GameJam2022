using UnityEngine;

public class Mob : MonoBehaviour {
  int Gold { get => 1 + Wave*2; }
  public int Wave = 0;

  public void DropGold() {
    var gold = (int)(Player.Get().GetComponent<Attributes>().GetValue(AttributeTag.GoldGain, Gold * Random.Range(.5f, 2f)));
    Coin.SpawnCoins(GetComponent<Defender>().LastGroundedPosition.Value.XZ() + new Vector3(0, 1f, 0), gold);
  }

  void OnDeath() {
    DropGold();
  }

  void Awake() {
    MobManager.Instance.Mobs.Add(this);
  }

  void OnDestroy() {
    MobManager.Instance.Mobs.Remove(this);
  }
}
