using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public override void Activate(Upgrades us) {
    us.AddAttributeModifier(Attribute, Modifier);
    base.Activate(us);
  }
  public override void Load(Upgrades us, UpgradeData data) {
    us.AddAttributeModifier(Attribute, Modifier);
    us.AddUpgrade(data);
  }
}