using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PelletAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Bullet BulletPrefab;
  public int NumBullets;
  public HitParams HitParams;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public GameObject HitVFX;
  public AudioClip HitSFX;

  public override IEnumerator Routine() {
    yield return Windup.Start(Animator, Index);
    var shoot = StartCoroutine(ShootRoutine());
    yield return Active.Start(Animator, Index);
    StopCoroutine(shoot);
    yield return Recovery.Start(Animator, Index);
  }

  IEnumerator ShootRoutine() {
    for (int i = 0; i < NumBullets; i++) {
      var framesRemaining = Active.Duration.Frames / NumBullets;
      while (--framesRemaining > 0)
        yield return new WaitForFixedUpdate();
      VFXManager.Instance.TrySpawnEffect(FireVFX, transform.position);
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      Bullet.Fire(BulletPrefab, transform.position, transform.forward, OnHit);
    }
  }

  public override void AfterEnd() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
  }

  void OnHit(Bullet bullet, Defender defender) {
    defender.OnHit(HitParams, bullet.transform);

    SFXManager.Instance.TryPlayOneShot(HitSFX);
    VFXManager.Instance.TrySpawnEffect(HitVFX, bullet.transform.position);
  }
}