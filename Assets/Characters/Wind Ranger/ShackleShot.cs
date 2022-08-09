using System.Collections;
using UnityEngine;

public class ShackleShot : Ability {
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;

  protected override IEnumerator MakeRoutine() {
    yield return Animator.Run(WindupClip);
    Debug.Log("SHACKLE SHOT!");
    yield return Animator.Run(ReleaseClip);
    Stop();
  }
}