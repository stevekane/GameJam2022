using System;
using System.Collections;
using UnityEngine;
using static Fiber;

namespace PigMoss {
  [Serializable]
  public class Bombard : FiberAbility {
    public float Radius;
    public HitConfig HitConfig;
    public Missile MissilePrefab;
    public Transform[] LaunchSites;
    public Timeval Windup = Timeval.FromSeconds(1);
    public Timeval ShotPeriod = Timeval.FromSeconds(.1f);
    public Timeval Recovery = Timeval.FromSeconds(1);
    public AudioClip WindupClip;
    public AudioClip ShotClip;
    public AudioClip RecoveryClip;
    public GameObject ShotEffect;
    public Animator Animator;

    public override float Score() {
      return Mathf.Clamp01(BlackBoard.DistanceScore/25f);
    }

    public override void OnStop() {
      Animator.SetBool("Extended", false);
    }

    public override IEnumerator Routine() {
      SFXManager.Instance.TryPlayOneShot(WindupClip);
      Animator.SetBool("Extended", true);
      yield return Wait(Windup);
      var hitParams = HitConfig.ComputeParams(Attributes);
      foreach (var launchSite in LaunchSites) {
        var missile = GameObject.Instantiate(MissilePrefab, launchSite.position, launchSite.rotation);
        missile.Target = Radius*UnityEngine.Random.onUnitSphere.XZ();
        missile.HitParams = hitParams;
        SFXManager.Instance.TryPlayOneShot(ShotClip);
        VFXManager.Instance.TrySpawnEffect(ShotEffect, launchSite.position);
        yield return Wait(ShotPeriod);
      }
      SFXManager.Instance.TryPlayOneShot(RecoveryClip);
      Animator.SetBool("Extended", false);
      yield return Wait(Recovery);
    }
  }
}