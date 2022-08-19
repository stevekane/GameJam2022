using System.Collections;
using UnityEngine;
using static Fiber;

public class FocusFire : Ability {
  public FocusFireArrow ArrowPrefab;
  public AnimationClip FireClip;
  public Animator Animator;
  public Transform Owner;
  public Transform Target;
  public Timeval Duration = Timeval.FromMillis(10000);
  public Timeval ShotCooldown = Timeval.FromMillis(100);
  public float MaxRange = 5;

  public IEnumerator MakeRoutine() {
    yield return Any(Wait(Duration.Frames), RapidFire(ShotCooldown.Frames));
    Stop();
  }

  IEnumerator RapidFire(int cooldown) {
    while (true) {
      yield return Animator.Run(FireClip);
      Instantiate(ArrowPrefab, transform.position, transform.rotation);
      yield return Wait(ShotCooldown.Frames);
      // if (Target && Vector3.Distance(Owner.position, Target.position) < MaxRange) {
      //   yield return Animator.Run(FireClip);
      //   Instantiate(ArrowPrefab, transform.position, transform.rotation);
      // } else {
      //   yield return null;
      // }
    }
  }
}