using UnityEngine;

[System.Serializable]
public class UpgradeAbilityListData : UpgradeData {
  public int CurrentLevel = 0;
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Ability")]
public class UpgradeAbilityList : Upgrade {
  [System.Serializable]
  public class Level {
    public AbilityTag AbilityTag;
    public int Cost = 0;
  }
  public Level[] Levels;
  UpgradeAbilityListData GetData(Upgrades us) => us.FindUpgrade(ud => ud.Upgrade == this) as UpgradeAbilityListData;
  public override void Add(Upgrades us) {
    if (GetData(us) is var ud && ud != null) {
      ud.CurrentLevel++;
      us.OnChanged();
    } else {
      us.AddUpgrade(new UpgradeAbilityListData() { Upgrade = this, CurrentLevel = 0 });
    }
  }
  public override void Apply(Upgrades us) => us.AbilityTags.AddFlags(Levels[GetData(us).CurrentLevel].AbilityTag);
  public override int GetCost(Upgrades us) => Levels.TryGetIndex(GetData(us).CurrentLevel+1, out Level lvl) ? lvl.Cost : int.MaxValue;
}
