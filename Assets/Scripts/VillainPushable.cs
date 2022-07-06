using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillainPushable : Pushable {
  Hero Hero;

  void Awake() {
    Hero = GetComponent<Hero>();
  }

  public override void Push(Vector3 velocity) {
    Hero?.Push(velocity);
  }
}