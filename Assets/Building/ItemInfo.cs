using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Crafting/Item")]
public class ItemInfo : ScriptableObject {
  [SerializeField] ItemObject ObjectPrefab;

  public ItemObject Spawn(Vector3 position) => Spawn(position, Quaternion.identity);
  public ItemObject Spawn(Vector3 position, Quaternion rotation) {
    var instance = Instantiate(ObjectPrefab, position, rotation);
    instance.Info = this;
    return instance;
  }
}
