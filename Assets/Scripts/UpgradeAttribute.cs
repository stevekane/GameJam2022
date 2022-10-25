using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public int Cost;
  public override void Buy(Upgrades us) => us.BuyUpgrade(new() { Upgrade = this }, Cost, true);
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Modifier);
}