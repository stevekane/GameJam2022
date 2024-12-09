using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

public class PauseAbility : Ability {
  public override bool CanStart(AbilityMethod func) => true;

  // Button press/release.
  public override async Task MainAction(TaskScope scope) {
    try {
      if (Time.timeScale < 1f) {
        Time.timeScale = 1f;
      } else {
        Time.timeScale = 0.01f;
      }
      //await scope.Any(ListenFor(MainRelease));
    } finally {
      //UnityEngine.Time.timeScale = 1.0f;
    }
  }
  //public virtual Task MainRelease(TaskScope scope) => null;
  protected override void FixedUpdate() {
    base.FixedUpdate();
  }
}