using UnityEngine;

public class ItemObject : MonoBehaviour {
  public ItemInfo Info { get; set; }
  public void MakePickupable() {
    var pickup = gameObject.AddComponent<Pickupable>();
    pickup.ItemObject = this;
  }
}