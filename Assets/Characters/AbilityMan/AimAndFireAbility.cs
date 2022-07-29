using System;
using System.Collections;
using UnityEngine;

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

  Coroutine Aim;
  AutoAimEffect AutoAimEffect;

  public override void BeforeBegin() {
    AutoAimEffect = new AutoAimEffect();
    Aimer.GetComponent<Status>()?.Add(AutoAimEffect);
    Animator.SetBool("Firing", true);
  }

  public override IEnumerator Routine() {
    Aim = StartCoroutine(new AimAt(Aimer, Target, TurnSpeed));
    yield return new WaitFrames(ShotCooldown.Frames);
    yield return NTimes(3, Fire);
  }

  public override void AfterEnd() {
    StopCoroutine(Aim);
    Animator.SetBool("Firing", false);
    Aimer.GetComponent<Status>()?.Remove(AutoAimEffect);
  }

  IEnumerator Fire() {
    Animator.SetTrigger("Fire");
    AudioSource.PlayOptionalOneShot(FireSound);
    Instantiate(ProjectilePrefab, Origin.transform.position, Aimer.transform.rotation)
    .AddForce(ProjectileForce*Aimer.transform.forward, ForceMode.Impulse);
    yield return new WaitFrames(ShotCooldown.Frames);
  }

  IEnumerator NTimes(int n, Func<IEnumerator> f) {
    for (var i = 0; i < n; i++) {
      yield return f();
    }
  }
}