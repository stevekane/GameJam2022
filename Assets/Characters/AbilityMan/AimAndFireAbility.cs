using System.Collections;
using UnityEngine;
using static Fiber;

public class AimAndFireAbility : Ability {
  public Transform Aimer;
  public Transform Target;
  public Transform Origin;
  public Rigidbody ProjectilePrefab;
  public float ProjectileForce;
  public float TurnSpeed;
  public AudioSource AudioSource;
  public AudioClip FireSound;
  public Timeval ShotCooldown;
  public Animator Animator;

  AutoAimEffect AutoAimEffect;

  public override void Activate() {
    AutoAimEffect = new AutoAimEffect();
    Aimer.GetComponent<Status>()?.Add(AutoAimEffect);
    Animator.SetBool("Firing", true);
    base.Activate();
  }

  public override void Stop() {
    if (AutoAimEffect != null)
      Aimer.GetComponent<Status>()?.Remove(AutoAimEffect);
    Animator.SetBool("Firing", false);
    base.Stop();
  }

  protected override IEnumerator MakeRoutine() {
    yield return Any(new AimAt(Aimer, Target, TurnSpeed), NTimes(3, Fire));
    Stop();
  }

  IEnumerator Fire() {
    yield return Wait(ShotCooldown.Frames);
    Animator.SetTrigger("Fire");
    AudioSource.PlayOptionalOneShot(FireSound);
    Instantiate(ProjectilePrefab, Origin.transform.position, Aimer.transform.rotation)
    .AddForce(ProjectileForce*Aimer.transform.forward, ForceMode.Impulse);
  }
}