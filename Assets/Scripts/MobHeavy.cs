using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobHeavy : Mob {
  public override void MeHitThing(GameObject thing, MeHitThingType type, Vector3 contactPos) {
    Debug.Assert(false); // Heavy can't be thrown.
  }

  public override void ThingHitMe(ThingHitMeType thing, Vector3 thingPos) {
    TakeDamage(); // TODO: angle
  }
}
