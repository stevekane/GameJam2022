using UnityEngine;

public class Pickupper : MonoBehaviour {
  Inventory Inventory;

  public void Pickup(ItemObject item) {
    Inventory.Add(item.Info);
  }

  void Awake() {
    this.InitComponentFromParent(out Inventory);
  }
}