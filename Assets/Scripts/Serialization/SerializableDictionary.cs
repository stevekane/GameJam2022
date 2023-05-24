using System;
using System.Collections.Generic;
using UnityEngine;

// Note: To use this, create a concrete subclass of this with TKey and TValue specified, e.g.
// [Serializable] public class DictionaryStringInt : SerializableDictionary<string, int> {}
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
  [SerializeField] List<TKey> KeyList = new List<TKey>();
  [SerializeField] List<TValue> ValueList = new List<TValue>();

  public SerializableDictionary() : base() { }
  public SerializableDictionary(Dictionary<TKey, TValue> other) : base(other) { }

  public void OnBeforeSerialize() {
    KeyList.Clear();
    ValueList.Clear();
    foreach (var kv in this) {
      KeyList.Add(kv.Key);
      ValueList.Add(kv.Value);
    }
  }

  public void OnAfterDeserialize() {
    this.Clear();

    if (KeyList.Count != ValueList.Count)
      throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

    for (int i = 0; i < KeyList.Count; i++)
      this.Add(KeyList[i], ValueList[i]);
  }
}