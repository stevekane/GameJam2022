using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PelletAbility : AbilityFibered {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhaseFiber Windup;
  public InactiveAttackPhaseFiber Active;
  public InactiveAttackPhaseFiber Recovery;
  public Bullet BulletPrefab;
  public int NumBullets;
  public HitParams HitParams;
  public GameObject FireVFX;
  public AudioClip FireSFX;
  public GameObject HitVFX;
  public AudioClip HitSFX;

  protected override IEnumerator MakeRoutine() {
    yield return Windup.Start(Animator, Index);
    yield return Fiber.All(ShootRoutine(), Active.Start(Animator, Index));
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  IEnumerator ShootRoutine() {
    for (int i = 0; i < NumBullets; i++) {
      yield return Fiber.Wait(Active.Duration.Frames / NumBullets);
      VFXManager.Instance.TrySpawnEffect(FireVFX, transform.position);
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      Bullet.Fire(BulletPrefab, transform.position, transform.forward, OnHit);
    }
  }

  public override void Stop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    base.Stop();
  }

  void OnHit(Bullet bullet, Defender defender) {
    defender.OnHit(HitParams, bullet.transform);

    SFXManager.Instance.TryPlayOneShot(HitSFX);
    VFXManager.Instance.TrySpawnEffect(HitVFX, bullet.transform.position);
  }
}