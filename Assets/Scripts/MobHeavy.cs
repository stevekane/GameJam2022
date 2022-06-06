using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobHeavy : Mob {
  public override void MeHitThing(GameObject thing, MeHitThingType type, Vector3 contactPos) {
    Debug.Assert(false); // Heavy can't be thrown.
  }

  public override void ThingHitMe(GameObject thing, ThingHitMeType type, Vector3 contactPos) {
    if (IsOnShieldSide(contactPos)) {
      // TODO: proper direction
      Vector3 dir = (transform.position - contactPos).normalized;
      var body = thing.GetComponent<Rigidbody>();
      body.velocity = new Vector3(0, 0, 0);
      body.AddForce(dir * 5f, ForceMode.Impulse);
    } else {
      TakeDamage();
    }
  }

  public void OnPounceTo(Ape ape) {
    if (IsOnShieldSide(ape.transform.position))
      return;
    //      ape.Blocked();
  }

  bool IsOnShieldSide(Vector3 pos) {
    return false;
  }
}
