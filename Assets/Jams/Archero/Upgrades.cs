using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Archero {
  // Base class for runtime data regarding an upgrade.
  [Serializable]
  public class UpgradeData {
    public Upgrade Upgrade;
    public int CurrentLevel = 0;
  }

  [Serializable]
  public struct ExperienceEvent {
    public int Experience;
    public int NextLevelExperience;
    public ExperienceEvent(int experience, int nextLevelExperience) {
      Experience = experience;
      NextLevelExperience = nextLevelExperience;
    }
  }

  public class Upgrades : MonoBehaviour {
    public Upgrade DebugAddUpgrade;

    public List<UpgradeData> Active = new();
    List<UpgradeData> Added = new();
    [Serializable] public class AttributeDictionary : SerializableDictionary<AttributeTag, AttributeModifier> { }
    public AttributeDictionary Modifiers = new ();
    bool Dirty = false;
    public int XP = 0;
    public int CurrentLevel = 1;
    public UnityEvent<ExperienceEvent> OnExperience;
    public UnityEvent<int> OnLevel;
    public int XPToNextLevel => 100 + 50*(CurrentLevel-1);
    public AttributeModifier GetModifier(AttributeTag attrib) => Modifiers.GetValueOrDefault(attrib, null);
    public void AddAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Add(Modifiers, attrib, modifier);
    public void RemoveAttributeModifier(AttributeTag attrib, AttributeModifier modifier) => AttributeModifier.Remove(Modifiers, attrib, modifier);
    public UpgradeData GetUpgradeData(Upgrade upgrade) => Active.Find(ud => ud.Upgrade == upgrade) ?? Added.Find(ud => ud.Upgrade == upgrade);

    void Start() {
      ChangeExperience(XP);
      ChangeLevel(CurrentLevel);
    }

    bool CanBuyUpgrade(Upgrade upgrade) {
      var ud = GetUpgradeData(upgrade);
      return (ud?.CurrentLevel ?? 0) < upgrade.MaxLevel;
    }

    public void BuyUpgrade(Upgrade upgrade) {
      AddUpgrade(upgrade);
    }

    public void AddUpgrade(Upgrade upgrade) {
      Dirty = true;

      if (GetUpgradeData(upgrade) is var data && data != null) {
        data.CurrentLevel++;
      } else {
        data = new() { Upgrade = upgrade, CurrentLevel = 1 };
        Added.Add(data);
      }
      upgrade.OnAdded(this, data.CurrentLevel);
    }

    public void RemoveUpgrade(Upgrade upgrade) {
      Dirty = true;
      if (GetUpgradeData(upgrade) is var data) {
        Added.Remove(data);
        Active.Remove(data);
      }
    }

    public void ChangeExperience(int experience) {
      XP = experience;
      var experienceEvent = new ExperienceEvent(XP, XPToNextLevel);
      OnExperience.Invoke(experienceEvent);
      BroadcastMessage("OnExperience", experienceEvent, SendMessageOptions.DontRequireReceiver);
    }

    public void ChangeLevel(int level) {
      CurrentLevel = level;
      OnLevel.Invoke(level);
      BroadcastMessage("OnLevel", CurrentLevel, SendMessageOptions.DontRequireReceiver);
    }

    public void CollectGold(int gold) {
      ChangeExperience(XP+gold);
    }

    public void MaybeLevelUp() {
      if (XP >= XPToNextLevel) {
        var newXP = XP - XPToNextLevel;
        ChangeLevel(CurrentLevel+1);
        ChangeExperience(newXP);
        UpgradeUI.Instance.Show(this, PickUpgrades());
      }
    }

    Upgrade[] PickUpgrades() {
      var availableUpgrades = GameManager.Instance.Upgrades.Where(u => CanBuyUpgrade(u)).ToList();
      availableUpgrades.Shuffle();
      return availableUpgrades.Take(3).ToArray();
    }

    void FixedUpdate() {
      if (DebugAddUpgrade != null) {
        AddUpgrade(DebugAddUpgrade);
        DebugAddUpgrade = null;
      }
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