using UnityEngine;

public class Pickup : MonoBehaviour {
  public Upgrade Upgrade;
  void OnTriggerEnter(Collider c) {
    if (c.GetComponent<Player>() && c.TryGetComponent(out Upgrades us))
      OnPickup(us);
  }
  void OnPickup(Upgrades us) {
    Upgrade.Add(us, purchase: false);
    Destroy(gameObject);
  }
}
