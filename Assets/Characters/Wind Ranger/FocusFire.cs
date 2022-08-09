using System.Collections;
using UnityEngine;
using static Fiber;

public class FocusFire : Ability {
  public Transform Owner;
  public Transform Target;
  public Timeval Duration = Timeval.FromMillis(10000);
  public Timeval ShotCooldown = Timeval.FromMillis(100);
  public float MaxRange = 5;

  protected override IEnumerator MakeRoutine() {
    yield return Any(Wait(Duration.Frames), RapidFire(ShotCooldown.Frames));
    Stop();
  }

  IEnumerator RapidFire(int cooldown) {
    while (true) {
      if (Target && Vector3.Distance(Owner.position, Target.position) < MaxRange) {
        Debug.Log("Pew");
        yield return Wait(cooldown);
      } else {
        yield return null;
      }
    }
  }
}