using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade", menuName = "Upgrade/Attribute")]
public class UpgradeAttribute : Upgrade {
  public SerializableEnum<AttributeTag> Attribute;
  public AttributeModifier Modifier;
  public int Cost;
  public override void Add(Upgrades us, bool purchase) => us.BuyUpgrade(new() { Upgrade = this }, purchase ? Cost : 0, true);
  public override void Apply(Upgrades us) => us.AddAttributeModifier(Attribute, Modifier);
}