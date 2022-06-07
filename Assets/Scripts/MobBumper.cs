using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobBumper : Mob {
  [System.Flags]
  public enum BumperSide {
    Front = 0x1,
    Right = 0x2,
    Back = 0x4,
    Left = 0x8,
  }
  public BumperSide ActiveBumpers;

  public void Start() {
    base.Start();
    var bumpers = GetComponentsInChildren<MobBumperArm>();
    Debug.Assert(bumpers.Length == 4);
    for (int i = 0; i < 4; i++) {
      if (((int)ActiveBumpers & (1<<i)) == 0) {
        bumpers[i].gameObject.SetActive(false);
      }
    }
  }

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
      player.Bump(player.transform.position, (player.transform.position - transform.position).normalized * 10f);
  }
  // TODO: OnPounceFrom = long jump?

  // Return true if `pos` is on a side with an active bumper.
  bool IsOnBumperSide(Vector3 pos) {
    Vector3 dir = (pos - transform.position).normalized;
    float angle = Mathf.Atan2(transform.forward.x, transform.forward.z) - Mathf.Atan2(dir.x, dir.z);
    int side = (int)AngleToSide(angle);
    return (((int)ActiveBumpers & side) != 0);
  }

  // Given angle [-pi, pi], return BumperSide that corresponds to that angle (relative to forward).
  BumperSide AngleToSide(float angle) {
    // Map angle from [-pi,pi] to [0,1], and rotate by 1/8 to lie in a quadrant.
    angle = (angle/(2*Mathf.PI) + 1f + 1/8f) % 1f;
    if (angle < 1/4f)
      return BumperSide.Front;
    else if (angle < 2/4f)
      return BumperSide.Left;
    else if (angle < 3/4f)
      return BumperSide.Back;
    else 
      return BumperSide.Right;
  }
}
