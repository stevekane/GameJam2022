using System.Collections;
using UnityEngine;
using static Fiber;

public class PowerShot : Ability {
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;
  public EventSource Release = new();

  protected override IEnumerator MakeRoutine() {
    yield return Any(Animator.Run(WindupClip), ListenFor(Release));
    Debug.Log("POWER SHOT!");
    yield return Animator.Run(ReleaseClip);
    Stop();
  }
}