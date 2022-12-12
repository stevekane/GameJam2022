using System;
using System.Collections;
using UnityEngine;
using static Fiber;


public abstract class AbilityTask : IEnumerator {
  public IEnumerator Enumerator;
  public object Current { get => Enumerator.Current; }
  public bool MoveNext() => Enumerator.MoveNext();
  public void Dispose() => Enumerator = null;
  public void Reset() => Enumerator = Routine();
  public abstract IEnumerator Routine();
}

[Serializable]
public class AimAt : AbilityTask {
  public Transform Aimer;
  public Transform Target;
  public float TurnSpeed;
  public AimAt(Transform aimer, Transform target, float turnSpeed) {
    Aimer = aimer;
    Target = target;
    TurnSpeed = turnSpeed;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    while (true) {
      var current = Aimer.rotation;
      var desired = Quaternion.LookRotation(Target.position.XZ()-Aimer.position.XZ(), Vector3.up);
      Aimer.rotation = Quaternion.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed);
      yield return null;
    }
  }
}

public class AimAndFireAbility : LegacyAbility {
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

  public override void OnStop() {
    Animator.SetBool("Firing", false);
  }

  public IEnumerator AttackStart() {
    AddStatusEffect(new SpeedFactorEffect(1f, 0f));
    Animator.SetBool("Firing", true);
    yield return Any(new AimAt(Aimer, Target, TurnSpeed), NTimes(3, Fire));
  }

  IEnumerator Fire() {
    yield return Wait(ShotCooldown.Ticks);
    Animator.SetTrigger("Fire");
    AudioSource.PlayOptionalOneShot(FireSound);
    Instantiate(ProjectilePrefab, Origin.transform.position, Aimer.transform.rotation)
    .AddForce(ProjectileForce*Aimer.transform.forward, ForceMode.Impulse);
  }
}