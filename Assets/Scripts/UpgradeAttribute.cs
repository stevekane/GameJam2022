using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public SerializableEnum<AttributeTag> Attribute;
  public AttributeModifier Modifier;
  public int Cost;
  public override int GetCost(Upgrades us) => Cost;
  public override UpgradeData Add(Upgrades us) => new() { Upgrade = this };
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Modifier);
}