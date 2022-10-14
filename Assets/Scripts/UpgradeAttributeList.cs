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
  UpgradeAttributeListData GetData(Upgrades us) => us.FindUpgrade(ud => ud.Upgrade == this) as UpgradeAttributeListData;
  public override void Add(Upgrades us) {
    if (GetData(us) is var ud && ud != null) {
      ud.CurrentLevel++;
      us.OnChanged();
    } else {
      us.AddUpgrade(new UpgradeAttributeListData() { Upgrade = this, CurrentLevel = 0 });
    }
  }
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Modifiers[GetData(us).CurrentLevel]);
}