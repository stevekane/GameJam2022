using System.Collections;
using UnityEngine;
using static Fiber;

public class PowerShot : Ability {
  public AnimationCurve DamageMultiplierFromDuration;
  public PowerShotArrow ArrowPrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;

  public IEnumerator MakeRoutine() {
    var windup = Animator.Run(WindupClip);
    var release = ListenFor(AbilityManager.GetEvent(Release));
    var timer = new Timer();
    using var scoped = Scoped(Bundle, timer);
    yield return Any(windup, release);
    var maxDuration = WindupClip.Milliseconds();
    var duration = timer.Value.Millis;
    var arrow = Instantiate(ArrowPrefab, transform.position, transform.rotation);
    var damageMultiplier = DamageMultiplierFromDuration.Evaluate(duration/maxDuration);
    arrow.Damage *= damageMultiplier;
    yield return Animator.Run(ReleaseClip);
    Stop();
  }

  public IEnumerator Release() => null;
}