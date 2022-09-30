// This sucks. Is there a better way to have serialized polymorphic objects in a game object?
public class PickupUpgradeAttribute : Pickup {
  public override Upgrade Upgrade { get => UpgradeAttribute; }
  public UpgradeAttribute UpgradeAttribute;
}
