using System;
using UnityEngine;

namespace Archero {
  [CreateAssetMenu(fileName = "Upgrade", menuName = "ArcheroUpgrade/Attribute")]
  public class UpgradeAttribute : Upgrade {
    public SerializableEnum<AttributeTag> Attribute;
    public AttributeModifier Modifier;
    public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, CalcModifier(GetData(us).CurrentLevel));
    public AttributeModifier CalcModifier(int level) {
      var total = new AttributeModifier();
      for (int i = 0; i < level; i++)
        total.Merge(Modifier);
      return total;
    }
    UpgradeData GetData(Upgrades us) => us.GetUpgradeData(this);
  }
}