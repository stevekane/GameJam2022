using UnityEngine;

[CreateAssetMenu(fileName = "CharacterProto", menuName = "Crafting/CharacterProto")]
public class CharacterProto : ItemProto {
  public GameObject CharacterPrefab;
  public override void OnCrafted(Crafter crafter) {
    Instantiate(CharacterPrefab, crafter.OutputPortPos, Quaternion.identity);
    crafter.ExtractItem(this, 1);
    crafter.RequestCraft();
  }
}