using System.Collections;
using UnityEngine;

public class ShackleShot : Ability {
  public ShackleShotShackle ShacklePrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;

  protected override IEnumerator MakeRoutine() {
    yield return Animator.Run(WindupClip);
    Instantiate(ShacklePrefab, transform.position, transform.rotation);
    yield return Animator.Run(ReleaseClip);
    Stop();
  }
}