using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobBumper : Mob {
  public override void MeHitThing(GameObject thing, MeHitThingType type, Vector3 contactPos) {
    if (type == MeHitThingType.Wall || type == MeHitThingType.Shield) {
      // TODO: need surface normal
      Vector3 dir = (transform.position - contactPos).normalized;
      var body = GetComponent<Rigidbody>();
      body.velocity = new Vector3(0, 0, 0);
      body.AddForce(dir * 5f, ForceMode.Impulse);
    } else {
      // do nothing (i.e. keep going)
    }
  }

  public override void ThingHitMe(GameObject thing, ThingHitMeType type, Vector3 contactPos) {
    if (IsOnBumperSide(contactPos)) {
      // TODO: proper direction
      Vector3 dir = (transform.position - contactPos).normalized;
      var body = thing.GetComponent<Rigidbody>();
      body.velocity = new Vector3(0, 0, 0);
      body.AddForce(dir * 5f, ForceMode.Impulse);
    } else {
      TakeDamage();
    }
  }

  public override void OnPounceTo(Hero player) {
    if (IsOnBumperSide(player.transform.position))
      return;
//      player.Bump();
  }
  // TODO: OnPounceFrom = long jump?

  bool IsOnBumperSide(Vector3 pos) {
    return false;
  }
}
