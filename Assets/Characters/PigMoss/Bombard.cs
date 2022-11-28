using System;
using System.Collections;
using UnityEngine;
using static Fiber;

[Serializable]
public class Bombard : FiberAbility {
  public float Radius;
  public Missile MissilePrefab;
  public Transform[] LaunchSites;
  public Timeval Windup = Timeval.FromSeconds(1);
  public Timeval ShotPeriod = Timeval.FromSeconds(.1f);
  public Timeval Recovery = Timeval.FromSeconds(1);
  public SendMessageOptions MessageOptions = SendMessageOptions.DontRequireReceiver;

  public override void OnStop() {
    AbilityManager.SendMessage("OnBombardStop", MessageOptions);
  }

  public override IEnumerator Routine() {
    AbilityManager.SendMessage("OnBombardWindup", MessageOptions);
    yield return Wait(Windup);
    foreach (var launchSite in LaunchSites) {
      var missile = GameObject.Instantiate(MissilePrefab, launchSite.position, launchSite.rotation);
      missile.Target = Radius*UnityEngine.Random.onUnitSphere.XZ();
      AbilityManager.SendMessage("OnBombardShot", launchSite, MessageOptions);
      yield return Wait(ShotPeriod);
    }
    AbilityManager.SendMessage("OnBombardRecovery", MessageOptions);
    yield return Wait(Recovery);
  }
}