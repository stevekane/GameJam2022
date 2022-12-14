using System;
using UnityEngine;

/*
Wraps enums for "stable" serialization via strings.
NOTE: Assumes -1 is invalid value. Do not use with enums where -1 is valid.
*/

[Serializable]
public class SerializableEnumBase { }

[Serializable]
public class SerializableEnum<T> : SerializableEnumBase, ISerializationCallbackReceiver where T : struct, Enum {
  [SerializeField] T Value = default(T);
  [SerializeField] string String;

  public static implicit operator T(SerializableEnum<T> e) => e.Value;
  public override string ToString() => Value.ToString();

  public void OnAfterDeserialize() {
    if (!Enum.TryParse(String, out T value)) {
      Debug.LogError($"Unknown enum value for {typeof(T).GetType()}: '{String}'={Value}");
      Value = (T)(object)(-1);  // this cast is lame
    } else {
      Value = value;
    }
  }

  public void OnBeforeSerialize() {
    if (Convert.ToInt32(Value) != -1)
      String = Value.ToString();
  }
}