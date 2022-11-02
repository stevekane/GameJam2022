using System.Collections;
using UnityEngine;
using static Fiber;

public class FocusFire : Ability {
  public FocusFireArrow ArrowPrefab;
  public AnimationClip FireClip;
  public LayerMask LayerMask;
  public QueryTriggerInteraction TriggerInteraction;
  public Animator Animator;
  public Transform Owner;
  public Transform Target;
  public Timeval Duration = Timeval.FromMillis(10000);
  public Timeval ShotCooldown = Timeval.FromMillis(100);
  public float MaxRange = 5;
  public HitConfig HitConfig;

  public IEnumerator MakeRoutine() {
    var target = Targeting.StandardTarget(Owner, MaxRange, LayerMask, TriggerInteraction, PhysicsBuffers.Colliders);
    if (target) {
      Target = target.transform;
      yield return Any(Wait(Duration.Frames), RapidFire(ShotCooldown.Frames));
    }
    Stop();
  }

  IEnumerator RapidFire(int cooldown) {
    while (true) {
      if (Target && Vector3.Distance(Owner.position, Target.position) < MaxRange) {
        yield return Animator.Run(FireClip);
        Owner.transform.LookAt(Target);
        var arrow = Instantiate(ArrowPrefab, transform.position, transform.rotation);
        arrow.HitParams = HitConfig.ComputeParams(Attributes);
        yield return Wait(ShotCooldown.Frames);
      } else {
        yield return null;
      }
    }
  }
}