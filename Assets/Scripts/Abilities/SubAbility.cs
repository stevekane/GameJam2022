using System.Threading.Tasks;

public class SubAbility : Ability {
  public AbilityMethodReference Action;
  public AbilityMethodReference Release;

  Ability Ability;
  AbilityMethod ActionMethod;
  AbilityMethod ReleaseMethod;

  public override AbilityTag ActiveTags => Tags | Ability.ActiveTags;

  public override bool CanStart(AbilityMethod func) => Ability.CanStart(func == MainAction ? ActionMethod : ReleaseMethod);
  public override Task MainAction(TaskScope scope) => ActionMethod != null ? Ability.Run(scope, ActionMethod) : null;
  public override Task MainRelease(TaskScope scope) => ReleaseMethod != null ? Ability.Run(scope, ReleaseMethod) : null;

  void Start() {
    Ability = Action.Ability;
    ActionMethod = Action.GetMethod();
    ReleaseMethod = Release.GetMethod();
  }
}