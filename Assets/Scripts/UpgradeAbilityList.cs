using UnityEngine;

[System.Serializable]
public class UpgradeAbilityListData : UpgradeData {
  public int CurrentLevel = 0;
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Ability")]
public class UpgradeAbilityList : Upgrade {
  public AbilityTag[] AbilityTags;
  UpgradeAbilityListData GetData(Upgrades us) => us.FindUpgrade(ud => ud.Upgrade == this) as UpgradeAbilityListData;
  public override void Add(Upgrades us) {
    if (GetData(us) is var ud && ud != null) {
      ud.CurrentLevel++;
      us.OnChanged();
    } else {
      us.AddUpgrade(new UpgradeAbilityListData() { Upgrade = this, CurrentLevel = 0 });
    }
  }
  public override void Apply(Upgrades us) => us.AbilityTags.AddFlags(AbilityTags[GetData(us).CurrentLevel]);
}
