using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum AttributeTag {
  Damage,
  Health,
  Knockback,
  MoveSpeed,
  TurnSpeed,
  AttackSpeed,
  SlamDamage,
  SuplexDamage,
}

public class AttributeInfo {
  public static AttributeInfo Instance = new();
  public Dictionary<AttributeTag, float> DefaultValues = new() {
    { AttributeTag.Damage, 10 },
    { AttributeTag.Health, 100 },
    { AttributeTag.Knockback, 10 },
    { AttributeTag.MoveSpeed, 20 },
    { AttributeTag.TurnSpeed, 1080 },
    { AttributeTag.AttackSpeed, 10 },
    { AttributeTag.SlamDamage, 10 },
    { AttributeTag.SuplexDamage, 10 } // TODO: I don't think this is right but the custom GUI cries w/o it
  };
  public Dictionary<AttributeTag, AttributeTag?> Parents = new() {
    { AttributeTag.SlamDamage, AttributeTag.Damage },
    { AttributeTag.SuplexDamage, AttributeTag.Damage },
  };
}

[Serializable]
public class AttributeModifier {
  public float BonusMult = 1;
  public float BonusAdd = 0;
  public float Apply(float baseValue) => (baseValue + BonusAdd) * BonusMult;
  public AttributeModifier Merge(AttributeModifier other) {
    BonusAdd += other.BonusAdd;
    BonusMult *= other.BonusMult;
    return this;
  }
  public AttributeModifier Remove(AttributeModifier other) {
    Debug.Assert(other.BonusMult != 0f, "Cannot remove a x0 modifier");
    BonusAdd -= other.BonusAdd;
    BonusMult /= other.BonusMult;
    return this;
  }
  public static void Add(Dictionary<AttributeTag, AttributeModifier> dict, AttributeTag attrib, AttributeModifier modifier) {
    var m = dict.GetOrAdd(attrib, () => new());
    m.Merge(modifier);
  }
  public static void Remove(Dictionary<AttributeTag, AttributeModifier> dict, AttributeTag attrib, AttributeModifier modifier) {
    var m = dict.GetValueOrDefault(attrib, null);
    m?.Remove(modifier);
  }
}

public class Attributes : MonoBehaviour {
  // Do we really need to centralize base values in one location?
  public AttributeBaseValues BaseValues;
  Optional<Upgrades> UpgradeManager;
  Optional<Status> Status;
  private void Awake() {
    UpgradeManager = GetComponent<Upgrades>();
    Status = GetComponent<Status>();
  }
  AttributeModifier GetModifier(AttributeTag attrib) {
    AttributeModifier modifier = new();
    AttributeTag? current = attrib;
    while (current != null) {
      if (UpgradeManager?.Value.GetModifier(attrib) is var mu && mu != null)
        modifier.Merge(mu);
      if (Status?.Value.GetModifier(attrib) is var ms && ms != null)
        modifier.Merge(ms);
      current = AttributeInfo.Instance.Parents.GetValueOrDefault(current.Value, null);
    }
    return modifier;
  }
  // TODO: remove this one
  public float GetValue(AttributeTag attrib, float baseValue) => GetModifier(attrib).Apply(baseValue);
  public float GetValue(AttributeTag attrib) => GetValue(attrib, BaseValues.Values[(int)attrib]);
}

[Serializable]
public class AttributeBaseValues {
  public List<float> Values = new();
}

[Serializable]
[CustomPropertyDrawer(typeof(AttributeBaseValues))]
public class AttributeBaseValuesPropertyDrawer : PropertyDrawer {
  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return Enum.GetValues(typeof(AttributeTag)).Length * 20;
  }
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var listProp = property.FindPropertyRelative("Values");
    var attribNames = Enum.GetNames(typeof(AttributeTag));
    var row = new Rect(position); row.height = 20;
    while (listProp.arraySize < attribNames.Length) {
      var i = listProp.arraySize;
      listProp.InsertArrayElementAtIndex(i);
      var e = listProp.GetArrayElementAtIndex(i);
      e.floatValue = AttributeInfo.Instance.DefaultValues[(AttributeTag)i];
    }
    for (int i = 0; i < attribNames.Length; i++) {
      var e = listProp.GetArrayElementAtIndex(i);
      var p1 = new Rect(row); p1.width /= 2;
      EditorGUI.LabelField(p1, attribNames[i]);
      var p2 = new Rect(row); p2.width /= 2; p2.x += p2.width;
      float val = EditorGUI.FloatField(p2, e.floatValue);
      if (val != e.floatValue) {
        Debug.Log($"Changed to {val}");
        e.floatValue = val;
      }
      row.y += row.height;
    }
  }
}
