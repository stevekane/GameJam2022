using System;
using System.Collections.Generic;
using UnityEngine;
using static SaveObject;

public class Inventory : MonoBehaviour, ISaveableComponent {
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

  public ILoadableComponent Save() => new Serialized {
    Items = Items,
  };
  class Serialized : ILoadableComponent {
    public ItemDictionary Items;
    public void Load(GameObject go) {
      go.GetComponent<Inventory>().Items = Items;
    }
  }

  void Awake() {
    if (TryGetComponent(out SaveObject save))
      save.RegisterSaveable(this);
  }

  void Awake() {
    GetComponent<SaveObject>()?.RegisterSaveable(this);
  }
}