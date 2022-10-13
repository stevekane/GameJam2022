using UnityEngine;

public class Pickup : MonoBehaviour {
  public Upgrade Upgrade;
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out Upgrades us))
      OnPickup(us);
  }
  void OnPickup(Upgrades us) {
    Upgrade.Activate(us);
    var a = (Upgrade as UpgradeAttributeList).Attribute;
    Debug.Log($"{a} is now {us.GetComponent<Attributes>().GetValue(a)}");
    Destroy(gameObject);
  }
}
