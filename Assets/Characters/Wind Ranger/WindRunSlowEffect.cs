public class WindRunSlowEffect : StatusEffect {
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
      s.MoveSpeedFactor *= 0.6f;
      Remaining--;
    }
  }
}