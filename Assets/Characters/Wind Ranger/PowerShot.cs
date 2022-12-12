using System.Collections;
using UnityEngine;

public class PowerShot : LegacyAbility {
  public PowerShotArrow ArrowPrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;

  public IEnumerator MakeRoutine() {
    yield return Animator.Run(WindupClip);
    Instantiate(ArrowPrefab, transform.position, transform.rotation);
    yield return Animator.Run(ReleaseClip);
  }

  public IEnumerator Release() => null;
}