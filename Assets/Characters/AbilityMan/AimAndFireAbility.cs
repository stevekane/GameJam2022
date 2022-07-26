using System;
using System.Collections;
using UnityEngine;

public class AimAndFireAbility : SimpleAbility {
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

  public override void BeforeBegin() {
    Animator.SetBool("Firing", true);
  }

  public override IEnumerator Routine() {
    var aim = StartCoroutine(new AimAt(Aimer, Target, TurnSpeed));
    yield return new WaitFrames(ShotCooldown.Frames);
    yield return NTimes(3, Fire);
    StopCoroutine(aim);
  }

  public override void AfterEnd() {
    Animator.SetBool("Firing", false);
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