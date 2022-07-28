using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PelletAbility : SimpleAbility {
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
    Windup.Reset();
    yield return Windup;
    Active.Reset();
    var shoot = StartCoroutine(ShootRoutine());
    yield return Active;
    StopCoroutine(shoot);
    Recovery.Reset();
    yield return Recovery;
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