using UnityEngine;

public class Mob : MonoBehaviour {
  int Gold { get => 1 + Wave*2; }
  public int Wave = 0;

  public void DropGold() {
    var gold = (int)(Gold*Random.Range(.5f, 2f));
    Coin.SpawnCoins(transform.position, gold);
  }

  void OnDeath() {
    DropGold();
  }
}
