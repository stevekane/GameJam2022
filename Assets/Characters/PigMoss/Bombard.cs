using System;
using System.Threading.Tasks;
using UnityEngine;

namespace PigMoss {
  [Serializable]
  public class Bombard : Ability {
    public float Radius;
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

    public async Task Routine(TaskScope scope) {
      try {
        SFXManager.Instance.TryPlayOneShot(WindupClip);
        Animator.SetBool("Extended", true);
        await scope.Delay(Windup);
        foreach (var launchSite in LaunchSites) {
          var missile = GameObject.Instantiate(MissilePrefab, launchSite.position, launchSite.rotation);
          missile.Target = Radius*UnityEngine.Random.onUnitSphere.XZ();
          SFXManager.Instance.TryPlayOneShot(ShotClip);
          VFXManager.Instance.TrySpawnEffect(ShotEffect, launchSite.position);
          await scope.Delay(ShotPeriod);
        }
        SFXManager.Instance.TryPlayOneShot(RecoveryClip);
        Animator.SetBool("Extended", false);
        await scope.Delay(Recovery);
      } finally {
        Animator.SetBool("Extended", false);
      }
    }
  }
}