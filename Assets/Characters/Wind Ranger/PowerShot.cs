using System.Collections;
using UnityEngine;
using static Fiber;

public class Timer : AbilityTask, IValue<Timeval> {
  public Timeval Value { get; } = Timeval.FromMillis(0);
  public Timer() {
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    while (true) {
      yield return null;
      Value.Millis+=Time.fixedDeltaTime;
    }
  }
}

public class PowerShot : Ability {
  public AnimationCurve DamageMultiplierFromDuration;
  public PowerShotArrow ArrowPrefab;
  public Animator Animator;
  public AnimationClip WindupClip;
  public AnimationClip ReleaseClip;
  public EventSource Release = new();

  protected override IEnumerator MakeRoutine() {
    var windup = Animator.Run(WindupClip);
    var keyRelease = ListenFor(Release);
    var timer = new Timer();
    yield return Any(Any(windup, keyRelease), timer);
    var maxDuration = WindupClip.length;
    var duration = timer.Value.Millis;
    var arrow = Instantiate(ArrowPrefab, transform.position, transform.rotation);
    var damageMultiplier = DamageMultiplierFromDuration.Evaluate(duration/maxDuration);
    arrow.Damage *= damageMultiplier;
    Debug.Log($"Duration: {duration} | MaxDuration {maxDuration} | Damage {arrow.Damage}");
    yield return Animator.Run(ReleaseClip);
    Stop();
  }
}