using System;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/AttributeList")]
  public class UpgradeAttributeList : Upgrade {
    [Serializable]
    public class Level {
      public AttributeModifier Modifier;
      public int Cost = 0;
    }
    public SerializableEnum<AttributeTag> Attribute;
    public Level[] Levels;
    public override int GetCost(Upgrades us) {
      var levelidx = GetData(us)?.CurrentLevel ?? -1;
      return Levels.GetIndexOrDefault(levelidx+1)?.Cost ?? int.MaxValue;
    }
    public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Levels[GetData(us).CurrentLevel].Modifier);

    UpgradeData GetData(Upgrades us) => us.GetUpgradeData(this);
  }
}