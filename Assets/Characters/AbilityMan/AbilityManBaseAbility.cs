using System.Collections;

public class AbilityManBaseAbility : Ability {
  public LightAttackAbility LightAttackAbility;
  public AimAndFireAbility AimAndFireAbility;

  public override void OnAbilityAction(AbilityManager manager, AbilityAction action) {
    switch (action) {
    case AbilityAction.R1JustDown:
      manager.TryRun(LightAttackAbility);
    break;
    case AbilityAction.R2JustDown:
      manager.TryRun(AimAndFireAbility);
    break;
    }
  }

  protected override IEnumerator MakeRoutine() {
    while (true) {
      yield return null;
    }
  }
}