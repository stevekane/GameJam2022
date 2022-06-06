using UnityEngine;
using UnityEngine.Events;

public class Mob : MonoBehaviour {
  public MobConfig Config;

  public enum MeHitThingType { Mob, Bumper, Wall, Shield };
  public virtual void MeHitThing(GameObject thing, MeHitThingType type, Vector3 thingPos) {
    TakeDamage();
  }

  public enum ThingHitMeType { Mob, Explosion };
  public virtual void ThingHitMe(ThingHitMeType thing, Vector3 thingPos) {
    TakeDamage();
  }

  public virtual void TakeDamage() {
    Destroy(gameObject);  // TODO: unify death anim stuff
  }
}