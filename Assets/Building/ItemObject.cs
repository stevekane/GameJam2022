using UnityEngine;

public class ItemObject : MonoBehaviour {
  public ItemProto Info { get; set; }
  public void MakePickupable() {
    var pickup = gameObject.AddComponent<Pickupable>();
    pickup.ItemObject = this;
  }
}