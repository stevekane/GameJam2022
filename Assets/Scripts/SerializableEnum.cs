using System;
using UnityEditor;
using UnityEngine;

// This class wraps enums for "safer" serialization in case enums are rearranged. Imagine the following:
//   enum MobTypes { biter, wasp, badger, smoke }
// now we want to delete `biter`:
//   enum MobTypes { wasp, badger, smoke }
// Any serialized MobTypes will break. This class fixes that problem by serializing the enum name instead of the value.
//
// NOTE: This class assumes that -1 is an invalid value. Do not use with enums where -1 is valid.


[Serializable]
public class SerializableEnumBase { }

[Serializable]
public class SerializableEnum<T> : SerializableEnumBase, ISerializationCallbackReceiver where T : struct, Enum {
  [SerializeField] T Value = default(T);
  [SerializeField] string String;

  public static implicit operator T(SerializableEnum<T> e) => e.Value;

  public void OnAfterDeserialize() {
    if (!Enum.TryParse(String, out T value)) {
      Debug.LogError($"Unknown enum value for {typeof(T).GetType().Name}: {String}");
      Value = (T)(object)(-1);  // this cast is lame
      return;
    }
    Value = value;
  }

  public void OnBeforeSerialize() {
    if (Convert.ToInt32(Value) != -1)
      String = Value.ToString();
  }
}

[CustomPropertyDrawer(typeof(SerializableEnumBase), true)]
public class SerializableEnumPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var valueProp = property.FindPropertyRelative("Value");
    var strProp = property.FindPropertyRelative("String");
    var newIndex = EditorGUI.Popup(position, valueProp.enumValueIndex, valueProp.enumDisplayNames);
    if (newIndex == valueProp.enumValueIndex)
      return;
    valueProp.enumValueIndex = newIndex;
    strProp.stringValue = valueProp.enumNames[newIndex];
    property.serializedObject.ApplyModifiedProperties();
    property.serializedObject.Update();
  }
}

