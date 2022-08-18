public class WindRunBoostEffect : StatusEffect {
  public override bool Merge(StatusEffect e) => true;
  public override void Apply(Status status) {
    status.IsHittable = false;
    status.MoveSpeedFactor *= 1.6f;
  }
}