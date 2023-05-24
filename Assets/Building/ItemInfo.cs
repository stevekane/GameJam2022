using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Crafting/ItemProto")]
public class ItemInfo : ScriptableObject {
  [SerializeField] ItemObject ObjectPrefab;

  public ItemObject Spawn(Vector3 position) => Spawn(position, Quaternion.identity);
  public ItemObject Spawn(Vector3 position, Quaternion rotation) {
    var instance = Instantiate(ObjectPrefab, position, rotation);
    instance.Info = this;
    return instance;
  }

  // Callback when the item is finished crafting.
  // Used by BuildPlots and units that spawn into the world rather than waiting for harvest.
  public virtual void OnCrafted(Crafter crafter) {
    // By default, crafters request the next craft and put the output up for harvesting.
    crafter.RequestCraft();
    crafter.RequestHarvestOutput();
  }
}