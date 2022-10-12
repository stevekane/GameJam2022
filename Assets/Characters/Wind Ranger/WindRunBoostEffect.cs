public class WindRunBoostEffect : StatusEffect {
  static AttributeModifier Modifier = new() { Mult = 1.6f };
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) {
    status.IsHittable = false;
    status.AddAttributeModifier(AttributeTag.MoveSpeed, Modifier);
  }
}