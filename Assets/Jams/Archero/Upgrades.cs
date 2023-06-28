using System;
using System.Collections.Generic;
using System.Linq;
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
    public int CurrentLevel = 1;
    public int XPToNextLevel => 100 + 50*(CurrentLevel-1);
    public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
    public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
    public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
    public UpgradeData GetUpgradeData(Upgrade upgrade) => Active.Find(ud => ud.Upgrade == upgrade) ?? Added.Find(ud => ud.Upgrade == upgrade);
    bool CanBuyUpgrade(Upgrade upgrade) {
      var ud = GetUpgradeData(upgrade);
      return (ud?.CurrentLevel ?? 0) < upgrade.MaxLevel;
    }
    public void BuyUpgrade(Upgrade upgrade) {
      XP = 0;
      AddUpgrade(upgrade);
    }
    public void AddUpgrade(Upgrade upgrade) {
      Dirty = true;
      if (upgrade.Add(this) is UpgradeData data)
        Added.Add(data);
    }
    public void CollectGold(int gold) {
      XP += gold;
    }
    public void MaybeLevelUp() {
      if (XP >= XPToNextLevel) {
        XP -= XPToNextLevel;
        CurrentLevel++;
        UpgradeUI.Instance.Show(this, PickUpgrades());
      }
    }

    Upgrade[] PickUpgrades() {
      var availableUpgrades = GameManager.Instance.Upgrades.Where(u => CanBuyUpgrade(u)).ToList();
      availableUpgrades.Shuffle();
      return availableUpgrades.Take(3).ToArray();
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