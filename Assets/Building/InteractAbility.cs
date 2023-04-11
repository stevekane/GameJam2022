using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class InteractAbility : Ability {
  Crafter InteractTarget;
  string[] Choices;

  InlineEffect InteractEffect = new(s => {
    s.Tags.AddFlags(AbilityTag.Interact);
  }, "Interact");
  InlineEffect StopEffect => new(s => {
    s.CanRotate = false;
    s.CanMove = false;
  }, "Build menu");

  public override bool CanStart(AbilityMethod func) => InteractTarget && 0 switch {
    _ when func == MainRelease => IsRunning,
    _ => !IsRunning,
  };
  // TODO(HACK): All press+release events share the trigger condition. This is needed because we don't have
  // a way to specify Release trigger conditions.
  public override TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerCondition;

  public override async Task MainAction(TaskScope scope) {
    try {
      using var stopped = Status.Add(StopEffect);
      Choices = InteractTarget.Recipes.Select(r => r.name).ToArray();
      Menu.Show(Choices);
      var which = await scope.Any(
        ListenFor(MainRelease),
        Waiter.Repeat(async s => {
          var selected = GetSelected();
          Menu.Select(selected);
          await scope.Tick();
        }));
      Stop();
      Menu.Hide();
      var selected = GetSelected();
      if (selected >= 0) {
        ItemFlowManager.Instance.AddCraftRequest(InteractTarget, InteractTarget.Recipes[selected]);
      }
    } finally {
    }
  }
  public Task RotateAction(TaskScope scope) {
    InteractTarget.transform.rotation *= Quaternion.AngleAxis(90f, Vector3.up);
    return null;
  }

  int GetSelected() => Menu.GetSelectedFromAim(AbilityManager.GetAxis(AxisTag.Move).XZ, Choices.Length);

  float InteractDist = 1f;
  void FixedUpdate() {
    var obj = BuildGrid.GetCellContents(Character.transform.position + Character.transform.forward*InteractDist);
    var couldInteract = InteractTarget != null;
    InteractTarget = obj?.GetComponent<Crafter>();
    if (InteractTarget && !couldInteract) {
      Status.Add(InteractEffect);
    } else if (!InteractTarget && couldInteract) {
      Status.Remove(InteractEffect);
    }
  }

  RadialMenuUI Menu;
  void Start() {
    Character.InitComponentFromChildren(out Menu);
  }
}