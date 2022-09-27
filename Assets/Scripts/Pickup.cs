using UnityEngine;

public class Pickup : MonoBehaviour {
  public virtual Upgrade Upgrade { get; }
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out UpgradeManager um))
      OnPickup(um);
  }
  void OnPickup(UpgradeManager um) {
    um.AddUpgrade(Upgrade);
    Destroy(this);
  }
}
