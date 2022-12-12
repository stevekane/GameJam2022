using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public AnimationClip DeathClip;
  public Hurtbox Hurtbox;
  AnimationDriver AnimationDriver;
  Damage Damage;
  TaskScope Scope = new();

  async Task WatchDamage(TaskScope scope) {
    await scope.While(() => Damage.Points < MaxDamage);
    Destroy(Hurtbox.gameObject);
    var job = AnimationDriver.Play(scope, DeathClip);
    await job.WaitDone(scope);
    Destroy(gameObject, .01f);
  }

  void Awake() {
    Damage = GetComponent<Damage>();
    AnimationDriver = GetComponentInParent<AnimationDriver>();
    Scope.Start(WatchDamage);
  }
  void OnDestroy() => Scope.Cancel();
}