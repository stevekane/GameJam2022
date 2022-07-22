using System.Collections;
using UnityEngine;

public class AimAndFireAbility : SimpleAbility {
  public Transform Aimer;
  public Transform Target;
  public Transform Origin;
  public Rigidbody ProjectilePrefab;
  public AudioSource AudioSource;
  public AudioClip FireSound;
  public Timeval ShotCooldown;
  public Animator Animator;
  public override IEnumerator Routine() {
    StartCoroutine(new AimAt { Aimer = Aimer, Target = Target }.Start());
    Animator.SetBool("Attacking", true);
    for (var i = 0; i < 3; i++) {
      yield return new WaitFrames(ShotCooldown.Frames).Start();
      Fire();
    }
    yield return new WaitFrames(ShotCooldown.Frames).Start();
    Animator.SetBool("Attacking", false);
  }
  void Fire() {
    Animator.SetTrigger("Attack");
    AudioSource.PlayOptionalOneShot(FireSound);
    Instantiate(ProjectilePrefab, Origin.transform.position, Aimer.transform.rotation)
    .AddForce(Aimer.transform.forward*100, ForceMode.Impulse);
  }
}