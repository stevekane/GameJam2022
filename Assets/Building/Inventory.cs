using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
  [Serializable] public class ItemDictionary : SerializableDictionary<ItemInfo, int> { }
  [SerializeField] ItemDictionary Items = new();

  public void Add(ItemInfo item, int count = 1) => Items[item] = Items.GetValueOrDefault(item) + count;
  public void MoveTo(Inventory other) {
    foreach (var kv in Items)
      other.Add(kv.Key, kv.Value);
    Items.Clear();
  }
}