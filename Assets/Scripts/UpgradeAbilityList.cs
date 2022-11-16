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
  public override void Add(Upgrades us, bool purchase) {
    var ud = GetData(us) ?? new UpgradeAbilityListData() { Upgrade = this, CurrentLevel = -1 };
    var isNew = ++ud.CurrentLevel == 0;
    us.BuyUpgrade(ud, purchase ? Levels[ud.CurrentLevel].Cost : 0, isNew);
  }
  public override void Apply(Upgrades us) => us.AbilityTags.AddFlags(Levels[GetData(us).CurrentLevel].AbilityTag);
}