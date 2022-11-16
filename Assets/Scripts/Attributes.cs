using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum AttributeTag {
  Damage,
  Health,
  Knockback,
  MoveSpeed,
  TurnSpeed,
  AttackSpeed,
  HasGravity,
  CanAttack,
  IsHittable,
  IsDamageable,
  GoldGain,
  FreezingHits,
  BurningHits,

  // Abilities
  AbilityStart = 1000,
  AbilityHeavyActive,
  AbilitySlamActive,
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

// Fuck you, Unity
[Serializable]
public class AttributeTagModifierPair {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
}

public class Attributes : MonoBehaviour {
  // These two are potentially confusing. They serve mostly as convenience. In general, the player will have a set of BaseUpgrades
  // where level 0 of each represents the initial value for his attributes. Mobs will use BaseAttributes that represent their default
  // values - only because that's an easier way to specify the values than creating a list of upgrades for each mob.
  public List<AttributeTagModifierPair> BaseAttributes;
  public List<Upgrade> BaseUpgrades;
  Dictionary<AttributeTag, AttributeModifier> BaseAttributesDict = new();
  Upgrades Upgrades;
  Optional<Status> Status;
  private void Awake() {
    Upgrades = this.GetOrCreateComponent<Upgrades>();
    Status = GetComponent<Status>();
    Debug.Assert(BaseAttributes.Count == 0 || BaseUpgrades.Count == 0, "BaseUpgrades will add to BaseAttributes, you probably only want one of these");
    BaseUpgrades.ForEach(u => Upgrades.AddUpgrade(u));
    BaseAttributes.ForEach(kv => BaseAttributesDict.Add(kv.Attribute, kv.Modifier));
  }
  AttributeModifier GetModifier(AttributeTag attrib) {
    AttributeModifier modifier = new();
    if (BaseAttributesDict.GetValueOrDefault(attrib, null) is var mb && mb != null)
      modifier.Merge(mb);
    if (Upgrades.GetModifier(attrib) is var mu && mu != null)
      modifier.Merge(mu);
    if (Status?.Value.GetModifier(attrib) is var ms && ms != null)
      modifier.Merge(ms);
    return modifier;
  }
  public float GetValue(AttributeTag attrib, float baseValue = 0f) => GetModifier(attrib).Apply(baseValue);
}
