using System;
using System.Threading.Tasks;
using UnityEngine;

namespace PigMoss {
  [Serializable]
  public class Bombard : Ability {
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

    public async Task Routine(TaskScope scope) {
      try {
        SFXManager.Instance.TryPlayOneShot(WindupClip);
        Animator.SetBool("Extended", true);
        await scope.Delay(Windup);
        foreach (var launchSite in LaunchSites) {
          TaskScope.Start(new Task(async () => await LaunchMissle(HitConfig, Attributes.serialized, Attributes.gameObject, MissilePrefab, launchSite, Radius, ShotClip, ShotEffect)));
          await scope.Delay(ShotPeriod);
        }
        SFXManager.Instance.TryPlayOneShot(RecoveryClip);
        Animator.SetBool("Extended", false);
        await scope.Delay(Recovery);
      } finally {
        Animator.SetBool("Extended", false);
      }
    }

    static async Task LaunchMissle(HitConfig hitConfig, Attributes.Serialized attributes, GameObject attacker, Missile misslePrefab, Transform launchSite, float radius, AudioClip sfx, GameObject vfx) {
      using TaskScope scope = new(GameManager.Instance.GlobalScope);
      Missile missile = null;
      TargetedStrike payload = null;

      try {
        // Launch missile.
        missile = Instantiate(misslePrefab, launchSite.position, launchSite.rotation);
        SFXManager.Instance.TryPlayOneShot(sfx);
        VFXManager.Instance.TrySpawnEffect(vfx, launchSite.position);
        await scope.Delay(missile.Duration);

        // Spawn payload.
        var target = radius*UnityEngine.Random.onUnitSphere.XZ();
        payload = Instantiate(missile.PayloadPrefab, target, Quaternion.identity).GetComponent<TargetedStrike>();
        Destroy(missile.gameObject);

        const float MAX_PROJECTION_DEPTH = 10;
        var totalTicks = payload.Duration.Ticks;
        for (var tick = 0; tick <= totalTicks; tick++) {
          var interpolant = (float)tick/(float)totalTicks;
          var pradius = Mathf.Lerp(payload.MinRadius, payload.MaxRadius, payload.Radius.Evaluate(interpolant));
          var alpha = Mathf.Lerp(payload.MinAlpha, payload.MaxAlpha, payload.Alpha.Evaluate(interpolant));
          payload.Projector.fadeFactor = alpha;
          payload.Projector.size = new Vector3(pradius, pradius, MAX_PROJECTION_DEPTH);
          await scope.Tick();
        }

        // Explode.
        var explosion = Instantiate(payload.SpawnPrefab, payload.transform.position, payload.transform.rotation).GetComponent<Explosion>();
        explosion.HitParams = new HitParams(hitConfig, attributes, attacker, explosion.gameObject);
        Destroy(explosion.gameObject, 5);
        Destroy(payload.gameObject);
      } catch (OperationCanceledException) {
      } finally {
        if (missile) Destroy(missile.gameObject);
        if (payload) Destroy(payload.gameObject);
      }
    }
  }
}
