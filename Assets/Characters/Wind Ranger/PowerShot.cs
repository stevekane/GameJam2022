using System.Collections;
using UnityEngine;
using static Fiber;

public class PowerShot : Ability {
  public AnimationCurve DamageMultiplierFromDuration;
  public PowerShotArrow ArrowPrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;
  public HitConfig HitConfig;

  public IEnumerator MakeRoutine() {
    var windup = Animator.Run(WindupClip);
    var release = ListenFor(AbilityManager.GetEvent(Release));
    var timer = new Timer();
    // Using(Scoped(Bundle, timer));
    yield return Any(windup, release);
    var maxDuration = WindupClip.Milliseconds();
    // var duration = timer.Value.Millis;
    var duration = maxDuration;
    var arrow = Instantiate(ArrowPrefab, transform.position, transform.rotation);
    arrow.HitParams = HitConfig.ComputeParams(Attributes);
    arrow.HitParams.Damage *= DamageMultiplierFromDuration.Evaluate(duration/maxDuration);
    yield return Animator.Run(ReleaseClip);
  }

  public IEnumerator Release() => null;
}