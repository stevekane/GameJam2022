using UnityEngine;

public class Pickup : MonoBehaviour {
  public virtual Upgrade Upgrade { get => null; }
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out Upgrades um))
      OnPickup(um);
  }
  void OnPickup(Upgrades um) {
    um.AddUpgrade(Upgrade);
    Destroy(gameObject);
  }
}
