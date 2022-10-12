public class WindRunSlowEffect : StatusEffect {
  static AttributeModifier Modifier = new() { Mult = 0.6f };
  public int Remaining;

  public WindRunSlowEffect(int remaining) {
    Remaining = remaining;
  }
  public override bool Merge(StatusEffect c) {
    Remaining = (c as WindRunSlowEffect).Remaining;
    return true;
  }
  public override void Apply(Status s) {
    if (Remaining <= 0) {
      s.Remove(this);
    } else {
      s.AddAttributeModifier(AttributeTag.MoveSpeed, Modifier);
      Remaining--;
    }
  }
}