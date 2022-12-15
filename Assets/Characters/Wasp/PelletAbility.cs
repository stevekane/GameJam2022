using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class PelletAbility : Ability {
  public int Index;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public InactiveAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public Bullet BulletPrefab;
  public int NumBullets;
  public GameObject FireVFX;
  public AudioClip FireSFX;

  public override async Task MainAction(TaskScope scope) {
    try {
      await Windup.Start(scope, Animator, Index);
      await scope.All(ShootRoutine, s => Active.Start(s, Animator, Index));
      await Recovery.Start(scope, Animator, Index);
    } finally {
      Animator.SetBool("Attacking", false);
      Animator.SetInteger("AttackIndex", -1);
      Animator.SetFloat("AttackSpeed", 1);
    }
  }

  async Task ShootRoutine(TaskScope scope) {
    for (int i = 0; i < NumBullets; i++) {
      await scope.Ticks(Active.Duration.Ticks / NumBullets);
      VFXManager.Instance.TrySpawnEffect(FireVFX, transform.position);
      SFXManager.Instance.TryPlayOneShot(FireSFX);
      Bullet.Fire(BulletPrefab, transform.position, transform.forward, gameObject.layer);
    }
  }
}