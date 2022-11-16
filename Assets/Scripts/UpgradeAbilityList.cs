using UnityEngine;


[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Ability")]
public class UpgradeAbilityList : Upgrade {
  [System.Serializable]
  public class MyUpgradeData : UpgradeData {
    public int CurrentLevel = 0;
  }
  [System.Serializable]
  public class Level {
    public AbilityTag AbilityTag;
    public int Cost = 0;
  }
  public Level[] Levels;

  MyUpgradeData GetData(Upgrades us) => us.GetUpgradeData(ud => ud.Upgrade == this) as MyUpgradeData;
  public override int GetCost(Upgrades us) {
    var levelidx = GetData(us)?.CurrentLevel ?? -1;
    return Levels.GetIndexOrDefault(levelidx+1)?.Cost ?? int.MaxValue;
  }
  public override UpgradeData Add(Upgrades us) {
    if (GetData(us) is MyUpgradeData ud) {
      ++ud.CurrentLevel;
      return null;
    }
    return new MyUpgradeData() { Upgrade = this, CurrentLevel = 0 };
  }
  public override void Apply(Upgrades us) => us.AbilityTags.AddFlags(Levels[GetData(us).CurrentLevel].AbilityTag);
}