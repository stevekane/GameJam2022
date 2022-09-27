public class ShackleShotEffect : StatusEffect {
  public override bool Merge(StatusEffect e) => false;
  public override void Apply(Status status) {
    status.CanMove = false;
    status.CanRotate = false;
    status.CanAttack = false;
  }
}