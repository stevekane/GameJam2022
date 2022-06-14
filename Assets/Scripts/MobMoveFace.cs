using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MobMoveFace : MobMove {
  MobConfig Config;
  Player Player;
  Vector3 LookDir;

  void Start() {
    Config = GetComponent<Mob>().Config;
    Player = GameObject.FindObjectOfType<Player>();
    LookDir = Vector3.forward;
  }

  RaycastHit[] hits = new RaycastHit[32];
  void FixedUpdate() {
    var delta = (Player.transform.position - transform.position).XZ();
    var numHits = Physics.RaycastNonAlloc(new Ray(transform.position, delta.normalized), hits, delta.magnitude);
    var anyObstacles = hits.Take(numHits).Any((hit) => hit.collider.GetComponentInParent<Player>() == null && hit.collider.GetComponentInParent<Mob>() == null);
    if (anyObstacles) {
      // Can't see player, rotate in place.
      LookDir = Quaternion.Euler(0, Config.TurnSpeedDeg * Time.deltaTime, 0) * LookDir;
    } else {
      LookDir = delta.normalized;
    }
    var targetRotation = Quaternion.LookRotation(LookDir, Vector3.up);
    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Config.TurnSpeedDeg * Time.deltaTime);
  }
}
