using System.Collections;
using UnityEngine;
using static Fiber;

public class PowerShot : Ability {
  public PowerShotArrow ArrowPrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;
  public EventSource Release = new();

  protected override IEnumerator MakeRoutine() {
    // TODO: Should modify the damage of the arrow based on duration
    // This may require a timer to track the charge duration...
    yield return Any(Animator.Run(WindupClip), ListenFor(Release));
    Instantiate(ArrowPrefab, transform.position, transform.rotation);
    yield return Animator.Run(ReleaseClip);
    Stop();
  }
}