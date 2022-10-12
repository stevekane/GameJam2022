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
  HasGravity,
  CanAttack,
  IsHittable,
  IsDamageable,
}

public class AttributeInfo {
  public static AttributeInfo Instance = new();
  // TODO: remove
  public Dictionary<AttributeTag, AttributeTag?> Parents = new() {
    { AttributeTag.SlamDamage, AttributeTag.Damage },
    { AttributeTag.SuplexDamage, AttributeTag.Damage },
  };
}

[Serializable]
public class AttributeModifier {
  public float Base = 0;
  public float Mult = 1;
  public float Apply(float baseValue) => (baseValue + Base) * Mult;
  public AttributeModifier Merge(AttributeModifier other) {
    Base += other.Base;
    Mult *= other.Mult;
    return this;
  }
  public AttributeModifier Remove(AttributeModifier other) {
    Debug.Assert(other.Mult != 0f, "Cannot remove a x0 modifier");
    Base -= other.Base;
    Mult /= other.Mult;
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
  public List<UpgradeAttributeList> BaseUpgrades;
  Optional<Upgrades> Upgrades;
  Optional<Status> Status;
  private void Awake() {
    Upgrades = this.GetOrCreateComponent<Upgrades>();
    Status = GetComponent<Status>();
    BaseUpgrades.ForEach(u => u.Activate(Upgrades.Value));
  }
  AttributeModifier GetModifier(AttributeTag attrib) {
    AttributeModifier modifier = new();
    AttributeTag? current = attrib;
    while (current != null) {
      if (Upgrades?.Value.GetModifier(attrib) is var mu && mu != null)
        modifier.Merge(mu);
      if (Status?.Value.GetModifier(attrib) is var ms && ms != null)
        modifier.Merge(ms);
      current = AttributeInfo.Instance.Parents.GetValueOrDefault(current.Value, null);
    }
    return modifier;
  }
  public float GetValue(AttributeTag attrib, float baseValue = 0f) => GetModifier(attrib).Apply(baseValue);
}
