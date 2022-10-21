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
  List<UpgradeData> Added = new();
  Dictionary<AttributeTag, AttributeModifier> Modifiers = new();
  bool Dirty = false;
  int Gold = 0;
// TODO: AbilityTag and Attributes are somewhat redundant. What do?
  public AbilityTag AbilityTags;
  public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
  public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
  public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
  public void AddUpgrade(UpgradeData upgrade) => Added.Add(upgrade);
  public UpgradeData FindUpgrade(Predicate<UpgradeData> pred) => Active.Find(pred);
  public void OnChanged() => Dirty = true;

  public void CollectGold(int gold) {
    Gold += gold;
  }

  public void Save(SaveData data) {
    data.Upgrades = Active;
    data.Gold = Gold;
  }
  public void Load(SaveData data) {
    Active.Clear();
    Modifiers.Clear();
    Added = data.Upgrades;
    Gold = data.Gold;
  }
  void FixedUpdate() {
    if (Added.Count > 0 || Dirty) {
      Dirty = false;
      Added.ForEach(e => Active.Add(e));
      Added.Clear();
      Modifiers.Clear();
      AbilityTags = 0;
      Active.ForEach(ud => ud.Upgrade.Apply(this));
      // TODO: remove test code
      // Active.ForEach(ud => {
      //   if (ud.Upgrade is UpgradeAttributeList ual)
      //     Debug.Log($"Upgrade: {this} {ual.Attribute} is now {GetComponent<Attributes>().GetValue(ual.Attribute)}");
      // });
    }
  }
}