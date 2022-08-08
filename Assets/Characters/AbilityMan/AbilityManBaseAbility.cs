using System.Collections;

public class AbilityManBaseAbility : Ability {
  public LightAttackAbility LightAttack;
  public AimAndFireAbility AimAndFire;
  public GrappleAbility Grapple;

  public override void Activate() {
    AbilityManager.R1JustDown.Action += TryRunLightAttack;
    AbilityManager.R2JustDown.Action += TryRunAimAndFire;
    AbilityManager.L2JustDown.Action += TryRunGrapple;
    base.Activate();
  }

  public override void Stop() {
    AbilityManager.R1JustDown.Action -= TryRunLightAttack;
    AbilityManager.R2JustDown.Action -= TryRunAimAndFire;
    AbilityManager.L2JustDown.Action -= TryRunGrapple;
    base.Stop();
  }

  protected override IEnumerator MakeRoutine() {
    while (true) {
      yield return null;
    }
  }

  void TryRunLightAttack() => AbilityManager.TryRun(LightAttack);
  void TryRunAimAndFire() => AbilityManager.TryRun(AimAndFire);
  void TryRunGrapple() => AbilityManager.TryRun(Grapple);
}