using System;
using System.Collections.Generic;
using UnityEngine;

// Base class for runtime data regarding an upgrade.
[Serializable]
public class UpgradeData {
  public Upgrade Upgrade;
}

public class Upgrades : MonoBehaviour {
  List<UpgradeData> Active = new();
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();
  public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
  public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
  public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
  public void AddUpgrade(UpgradeData upgrade) => Active.Add(upgrade);
  public UpgradeData FindUpgrade(Predicate<UpgradeData> pred) => Active.Find(pred);

  public void Save(SaveData data) {
    data.Upgrades = Active;
  }
  public void Load(SaveData data) {
    Active.Clear();
    Modifiers.Clear();
    // TODO: Ugh... this sucks. Load vs Activate? Find a way to merge those.
    data.Upgrades.ForEach(ud => ud.Upgrade.Load(this, ud));
  }
}