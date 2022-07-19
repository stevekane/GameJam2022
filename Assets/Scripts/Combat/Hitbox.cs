using UnityEngine;

public class Hitbox : MonoBehaviour {
  public Attacker Attacker { get; set; }
  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      Attacker.Hit(hurtbox);
    }
  }
}