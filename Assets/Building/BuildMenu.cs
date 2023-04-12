using System.Threading.Tasks;
using UnityEngine;

public class BuildMenu : Ability {
  // TODO: Not the best way to do this, but fine for now.
  [SerializeField] BuildObject[] Buildings;
  string[] Choices;
  BuildAbility BuildAbility;

  InlineEffect StopEffect => new(s => {
    s.CanRotate = false;
    s.CanMove = false;
  }, "Build menu");

  public override async Task MainAction(TaskScope scope) {
    try {
      using var stopped = Status.Add(StopEffect);
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
      if (selected == Buildings.Length) {
        BuildAbility.SetDeleteMode();
        AbilityManager.TryInvoke(BuildAbility.MainAction);
      } else if (selected >= 0) {
        BuildAbility.SetBuildPrefab(Buildings[selected]);
        AbilityManager.TryInvoke(BuildAbility.MainAction);
      }
    } finally { 
    }
  }

  int GetSelected() {
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    if (dir == Vector3.zero)
      return -1;
    var angle = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
    angle += 90f / Choices.Length;  // Offset the start region for the choices by the width of the region
    var frac = (1f + angle/360f) % 1f;
    var idx = (int)(frac * Choices.Length);
    return idx;
  }

  RadialMenuUI Menu;
  void Start() {
    Character.InitComponentFromChildren(out BuildAbility);
    Character.InitComponentFromChildren(out Menu);
    Choices = new string[Buildings.Length+1];
    Buildings.ForEach((b, i) => Choices[i] = b.name );
    Choices[Buildings.Length] = "Delete";
  }
}