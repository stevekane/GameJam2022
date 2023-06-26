using System;
using System.Collections.Generic;
using System.Xml.XPath;
using UnityEngine;

namespace Archero {
  // Base class for runtime data regarding an upgrade.
  [Serializable]
  public class UpgradeData {
    public Upgrade Upgrade;
    public int CurrentLevel = 0;
  }

  public class Upgrades : MonoBehaviour {
    List<UpgradeData> Active = new();
    List<UpgradeData> Added = new();
    Dictionary<AttributeTag, AttributeModifier> Modifiers = new();
    bool Dirty = false;
    public int XP = 0;
    public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
    public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
    public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
    public UpgradeData GetUpgradeData(Upgrade upgrade) => Active.Find(ud => ud.Upgrade == upgrade) ?? Added.Find(ud => ud.Upgrade == upgrade);
    public void BuyUpgrade(Upgrade upgrade) {
      XP = 0;
      AddUpgrade(upgrade);
    }
    public void AddUpgrade(Upgrade upgrade) {
      Dirty = true;
      if (GetUpgradeData(upgrade) is UpgradeData data) {
        data.CurrentLevel++;
      } else {
        Added.Add(new() { Upgrade = upgrade, CurrentLevel = 0 });
      }
    }
    public void CollectGold(int gold) {
      XP += gold;
    }

    void FixedUpdate() {
      if (Added.Count > 0 || Dirty) {
        Dirty = false;
        Added.ForEach(e => Active.Add(e));
        Added.Clear();
        Modifiers.Clear();
        Active.ForEach(ud => ud.Upgrade.Apply(this));
      }
    }
  }
 }