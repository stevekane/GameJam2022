using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public override void Activate(Upgrades us) {
    us.AddAttributeModifier(Attribute, Modifier);
    base.Activate(us);
  }
}