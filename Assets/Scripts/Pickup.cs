using UnityEngine;

public class Pickup : MonoBehaviour {
  public Upgrade Upgrade;
  void OnTriggerEnter(Collider c) {
    if (c.GetComponent<Player>() && c.TryGetComponent(out Upgrades us))
      OnPickup(us);
  }
  void OnPickup(Upgrades us) {
    // Dumb way to check if the upgrade is max level. This shouldn't really be a thing in the real game.
    if (Upgrade.GetCost(us) < int.MaxValue)
      us.AddUpgrade(Upgrade);
    Destroy(gameObject);
  }
}
