using System.Threading.Tasks;
using UnityEngine;

public class Shield : MonoBehaviour {
  public float MaxDamage = 50f;
  public AnimationJobConfig DeathAnimation;
  public Hurtbox Hurtbox;
  AnimationDriver AnimationDriver;
  Damage Damage;
  TaskScope Scope = new();

  public bool HurtboxEnabled {
    get => Hurtbox && Hurtbox.enabled;
    set { if (Hurtbox) Hurtbox.gameObject.SetActive(value); }
  }

  async Task WatchDamage(TaskScope scope) {
    await scope.While(() => Damage.Points < MaxDamage);
    Destroy(Hurtbox.gameObject);
    var job = AnimationDriver.Play(scope, DeathAnimation);
    await job.WaitDone(scope);
    Destroy(gameObject, .01f);
  }

  void Awake() {
    this.InitComponent(out Damage);
    this.InitComponent(out AnimationDriver);
    Scope.Start(WatchDamage);
  }
  void OnDestroy() => Scope.Cancel();
}