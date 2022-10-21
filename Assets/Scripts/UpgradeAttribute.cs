using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public AttributeTag Attribute;
  public AttributeModifier Modifier;
  public int Cost;
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Modifier);
  public override int GetCost(Upgrades us) => Cost;
}