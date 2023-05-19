using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public interface IInteractable {
  string[] Choices { get; }
  void Choose(Character interacter, int choiceIdx);
  void Rotate(float degrees);
}

public class InteractAbility : Ability {
  IInteractable InteractTarget;
  GameObject InteractIndicator;
  string[] Choices;

  InlineEffect InteractEffect = new(s => {
    s.Tags.AddFlags(AbilityTag.Interact);
  }, "Interact");
  InlineEffect StopEffect => new(s => {
    s.CanRotate = false;
    s.CanMove = false;
  }, "Build menu");

  public override bool CanStart(AbilityMethod func) =>
    InteractTarget != null &&
    0 switch {
      _ when func == MainRelease => IsRunning,
      _ => !IsRunning,
    };
  // TODO(HACK): All press+release events share the trigger condition. This is needed because we don't have
  // a way to specify Release trigger conditions.
  public override TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerCondition;

  public override async Task MainAction(TaskScope scope) {
    try {
      using var stopped = Status.Add(StopEffect);
      Choices = InteractTarget.Choices;
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
        InteractTarget.Choose(Character, selected);
      }
    } finally {
    }
  }
  public Task RotateAction(TaskScope scope) {
    InteractTarget.Rotate(90f);
    return null;
  }

  int GetSelected() => Menu.GetSelectedFromAim(AbilityManager.GetAxis(AxisTag.Move).XZ, Choices.Length);

  float InteractDist = 1f;
  void FixedUpdate() {
    var couldInteract = InteractTarget != null;
    var obj = BuildGrid.GetCellContents(Character.transform.position + Character.transform.forward*InteractDist);
    if (AbilityManager.Running.Any(a => a != this && a.ActiveTags.HasAllFlags(AbilityTag.OnlyOne))) {
      // TODO(HACK): We don't want to interact when there's another ability running.
      InteractTarget = null;
    } else {
      InteractTarget = obj?.GetComponent<Crafter>();
      if (InteractTarget == null)
        InteractTarget = obj?.GetComponent<Container>();
    }
    if (InteractTarget != null && !couldInteract) {
      InteractIndicator = Instantiate(VFXManager.Instance.DebugIndicatorPrefab, obj.transform.position + 3f*Vector3.up, Quaternion.identity);
      Status.Add(InteractEffect);
    } else if (InteractTarget == null && couldInteract) {
      InteractIndicator?.Destroy();
      Status.Remove(InteractEffect);
    }
  }

  RadialMenuUI Menu;
  void Start() {
    Character.InitComponentFromChildren(out Menu);
  }
}