using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeAttributeListData : UpgradeData {
  public int CurrentLevel = 0;
}


[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/AttributeList")]
public class UpgradeAttributeList : Upgrade {
  [System.Serializable]
  public class Level {
    public AttributeModifier Modifier;
    public int Cost = 0;
  }
  public AttributeTag Attribute;
  public Level[] Levels;
  UpgradeAttributeListData GetData(Upgrades us) => us.FindUpgrade(ud => ud.Upgrade == this) as UpgradeAttributeListData;
  public override void Add(Upgrades us) {
    if (GetData(us) is var ud && ud != null) {
      ud.CurrentLevel++;
      us.OnChanged();
    } else {
      us.AddUpgrade(new UpgradeAttributeListData() { Upgrade = this, CurrentLevel = 0 });
    }
  }
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Levels[GetData(us).CurrentLevel].Modifier);
  public override int GetCost(Upgrades us) => Levels.TryGetIndex(GetData(us).CurrentLevel+1, out Level lvl) ? lvl.Cost : int.MaxValue;
  public override string GetDescription(Upgrades us) {
    var lvl = Levels[GetData(us).CurrentLevel];
    var nextLvl = Levels[GetData(us).CurrentLevel+1];
    return $"{Attribute}\n{FormatModifier(lvl.Modifier)}\n=>\n{FormatModifier(nextLvl.Modifier)}";
  }
  string FormatModifier(AttributeModifier m) {
    return $"{m.Base} x{m.Mult}";
  }
}