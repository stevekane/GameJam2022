using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackAbility : Ability {
  public int Index;
  public Transform Owner;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public HitboxAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public GameObject HitVFX;
  public AudioClip HitSFX;
  public Vector3 HitVFXOffset = Vector3.up;
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitDamage;
  public float HitTargetKnockbackStrength;
  public float HitRecoilStrength;

  protected override IEnumerator MakeRoutine() {
    yield return Windup.Start(Animator, Index);
    // TODO: What if we wanted to do the inline ListenFor style?
    Active.OnContactStart.Action = OnHitStopStart;
    Active.OnContactEnd.Action = OnHitStopEnd;
    yield return Active.Start(Owner, Animator, Index);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public override void Stop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    base.Stop();
  }

  public void OnHitStopStart((Transform attacker, List<Transform> targets, int stopFrames) arg) {
    var hitStop = new HitStopEffect(arg.attacker.forward, HitStopVibrationAmplitude, arg.stopFrames);
    Owner.GetComponent<Status>()?.Add(hitStop);
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    arg.targets.ForEach(target => {
      var hitParams = new HitParams {
        HitStopDuration = Active.HitFreezeDuration,
        Damage = HitDamage,
        KnockbackStrength = HitTargetKnockbackStrength,
        KnockbackType = KnockBackType.Delta
      };
      target.GetComponent<Defender>()?.OnHit(hitParams, arg.attacker);
      VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+HitVFXOffset);
    });
  }

  public void OnHitStopEnd((Transform attacker, List<Transform> targets) arg) {
    arg.attacker.GetComponent<Status>()?.Add(new RecoilEffect(HitRecoilStrength * -arg.attacker.forward));
  }
}