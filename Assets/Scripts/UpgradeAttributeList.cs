using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/AttributeList")]
public class UpgradeAttributeList : Upgrade {
  [Serializable]
  public class MyUpgradeData : UpgradeData {
    public int CurrentLevel = 0;
  }
  [Serializable]
  public class Level {
    public AttributeModifier Modifier;
    public int Cost = 0;
  }
  public SerializableEnum<AttributeTag> Attribute;
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
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Levels[GetData(us).CurrentLevel].Modifier);
  public override UpgradeDescription GetDescription(Upgrades us) {
    var levelidx = GetData(us)?.CurrentLevel ?? -1;
    var currentModifier = levelidx == -1 ? new() : Levels[levelidx].Modifier;
    if (Levels.TryGetIndex(levelidx+1, out Level nextLevel)) {
      return new() {
        CurrentLevel = levelidx,
        Cost = nextLevel.Cost,
        Title = Attribute.ToString(),
        CurrentEffect = FormatModifier(currentModifier),
        NextEffect = FormatModifier(nextLevel.Modifier),
      };
    } else {
      return new() {
        CurrentLevel = levelidx,
        Cost = int.MaxValue,
        Title = Attribute.ToString(),
        CurrentEffect = FormatModifier(currentModifier),
        NextEffect = "MAXED",
      };
    }
  }
  string FormatModifier(AttributeModifier m) {
    return $"{m.Base} x{m.Mult}";
  }
}