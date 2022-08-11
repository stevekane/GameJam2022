using System.Collections;

public class AbilityManBaseAbility : Ability {
  public LightAttackAbility LightAttack;
  public AimAndFireAbility AimAndFire;
  public GrappleAbility Grapple;

  public override void Activate() {
    AbilityManager = GetComponentInParent<AbilityManager>();
    InputManager.Instance.ButtonEvent(ButtonCode.R1, ButtonPressType.JustDown).Action += TryRunLightAttack;
    InputManager.Instance.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Action += TryRunAimAndFire;
    InputManager.Instance.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Action += TryRunGrapple;
    base.Activate();
  }

  public override void Stop() {
    InputManager.Instance.ButtonEvent(ButtonCode.R1, ButtonPressType.JustDown).Action -= TryRunLightAttack;
    InputManager.Instance.ButtonEvent(ButtonCode.R2, ButtonPressType.JustDown).Action -= TryRunAimAndFire;
    InputManager.Instance.ButtonEvent(ButtonCode.L2, ButtonPressType.JustDown).Action -= TryRunGrapple;
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