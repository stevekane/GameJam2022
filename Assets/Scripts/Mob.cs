using UnityEngine;
using UnityEngine.Events;

public class Mob : MonoBehaviour {
  public MobConfig Config;

  public virtual void TakeDamage() {
    Destroy(gameObject, .1f);  // TODO: unify death anim stuff
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
  public virtual void OnPounceFrom(Hero hero) {
    Debug.Log($"PouncedFrom: {this} taking damage");
    TakeDamage();
  }
}