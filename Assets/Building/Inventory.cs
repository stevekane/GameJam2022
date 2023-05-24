using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour {
  [Serializable] public class ItemDictionary : SerializableDictionary<ItemProto, int> { }
  [SerializeField] ItemDictionary Items = new();

  public ItemDictionary Contents => Items;

  public int Count(ItemProto item) => Items.GetValueOrDefault(item);
  public void Add(ItemProto item, int count = 1) => Items[item] = Items.GetValueOrDefault(item) + count;
  public void Remove(ItemProto item, int count = 1) => Items[item] = Items.GetValueOrDefault(item) - count;
  public void MoveTo(Inventory other) {
    foreach (var kv in Items)
      other.Add(kv.Key, kv.Value);
    Items.Clear();
  }
}