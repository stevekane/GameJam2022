using System.Collections.Generic;
using UnityEngine;

public class UpgradeAttributeListData : UpgradeData {
  public int CurrentLevel = 0;
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/AttributeList")]
public class UpgradeAttributeList : Upgrade {
  public AttributeTag Attribute;
  public List<AttributeModifier> Modifiers;
  public override void Activate(Upgrades us) {
    var ud = us.FindUpgrade((ud) => ud.Upgrade == this) as UpgradeAttributeListData;
    if (ud == null)
      us.AddUpgrade(ud = new() { Upgrade = this, CurrentLevel = -1 });
    if (ud.CurrentLevel >= 0)
      us.RemoveAttributeModifier(Attribute, Modifiers[ud.CurrentLevel]);
    us.AddAttributeModifier(Attribute, Modifiers[++ud.CurrentLevel]);
  }
}