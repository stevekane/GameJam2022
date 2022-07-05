using UnityEngine;

public class HeroPushable : Pushable {
  Hero Hero;

  void Awake() {
    Hero = GetComponent<Hero>();
  }

  public override void Push(Vector3 velocity) {
    Hero.Push(velocity);
  }
}