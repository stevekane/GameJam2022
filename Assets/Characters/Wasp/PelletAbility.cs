using System.Collections;
using UnityEngine;

public class PelletAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Bullet BulletPrefab;
  public int NumBullets;
  public HitConfig HitConfig;
  public GameObject FireVFX;
  public AudioClip FireSFX;

  public IEnumerator AttackStart() {
    yield return Windup.Start(Animator, Index);
    yield return Fiber.All(ShootRoutine(), Active.Start(Animator, Index));
    yield return Recovery.Start(Animator, Index);
  }

  IEnumerator ShootRoutine() {
    for (int i = 0; i < NumBullets; i++) {
      yield return Fiber.Wait(Active.Duration.Ticks / NumBullets);
      VFXManager.Instance.TrySpawnEffect(FireVFX, transform.position);
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      Bullet.Fire(BulletPrefab, transform.position, transform.forward, HitConfig.ComputeParams(GetComponentInParent<Attributes>()), gameObject.layer);
    }
  }

  public override void OnStop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
  }
}