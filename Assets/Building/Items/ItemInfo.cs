using UnityEngine;
using static UnityEditor.Progress;

[CreateAssetMenu(fileName = "Item", menuName = "Crafting/Item")]
public class ItemInfo : ScriptableObject {
  [SerializeField] ItemObject ObjectPrefab;

  public ItemObject Spawn(Vector3 position) {
    var instance = Instantiate(ObjectPrefab, position, Quaternion.identity);
    instance.Info = this;
    return instance;
  }
}
