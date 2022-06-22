using UnityEngine;

public class Mob : MonoBehaviour {
  [SerializeField] internal AudioSource AttackAudioSource;
  [SerializeField] internal Animator Animator;

  public MobConfig Config;

  public void Start() {
  }

  public virtual void TakeDamage() {
    Destroy(GetComponent<MobMove>());
    Animator.SetTrigger("Die");
  }

  public virtual void Die() {
    Destroy(gameObject, .1f);
  }

  // Throwable
  public enum MeHitThingType { Mob, Bumper, Wall, Shield };
  public virtual void MeHitThing(GameObject thing, MeHitThingType type, Vector3 thingPos) {
    TakeDamage();
  }

  // Hittable
  public enum ThingHitMeType { Mob, Explosion };
  public virtual void ThingHitMe(GameObject thing, ThingHitMeType type, Vector3 thingPos) {
    TakeDamage();
  }

  // Targetable
  public virtual void OnPounceTo(Hero hero) { }
  public virtual void OnPounceFrom(Hero hero) { }
}