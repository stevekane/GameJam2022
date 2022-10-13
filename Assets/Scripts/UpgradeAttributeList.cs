using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeAttributeListData : UpgradeData {
  public int CurrentLevel = 0;
}

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/AttributeList")]
public class UpgradeAttributeList : Upgrade {
  public AttributeTag Attribute;
  public List<AttributeModifier> Modifiers;
  public override void Activate(Upgrades us) {
    var ud = us.FindUpgrade(ud => ud.Upgrade == this) as UpgradeAttributeListData;
    if (ud == null)
      us.AddUpgrade(ud = new() { Upgrade = this, CurrentLevel = -1 });
    if (ud.CurrentLevel >= 0)
      us.RemoveAttributeModifier(Attribute, Modifiers[ud.CurrentLevel]);
    us.AddAttributeModifier(Attribute, Modifiers[++ud.CurrentLevel]);
  }
  public override void Load(Upgrades us, UpgradeData data) {
    us.AddAttributeModifier(Attribute, Modifiers[((UpgradeAttributeListData)data).CurrentLevel]);
    us.AddUpgrade(data);
  }
}