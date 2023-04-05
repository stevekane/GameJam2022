using System;
using System.Threading.Tasks;
using UnityEngine;

public class BuildMenu : Ability {
  // TODO: Not the best way to do this, but fine for now.
  [SerializeField] BuildObject[] Buildings;
  BuildAbility BuildAbility;

  InlineEffect StopEffect => new(s => {
    s.CanRotate = false;
    s.CanMove = false;
  }, "Build menu");

  public override async Task MainAction(TaskScope scope) {
    try {
      using var stopped = Status.Add(StopEffect);
      Menu.Show(Buildings);
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
    var frac = (1f + angle/360f) % 1f;
    var idx = (int)(frac * Buildings.Length);
    return idx;
    //return Abilities[idx];
  }

  BuildMenuUI Menu;
  void Start() {
    Character.InitComponentFromChildren(out BuildAbility);
    this.InitComponentFromChildren(out Menu);
  }
}