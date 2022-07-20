using UnityEngine;

public class Hitbox : MonoBehaviour {
  public Attack Attack { get; set; }
  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      if (hurtbox.Defender.IsParrying) {
        hurtbox.Defender.OnParry(Attack);
        Attack.Attacker.OnParry(Attack, hurtbox.Defender);
      } else if (hurtbox.Defender.IsBlocking) {
        hurtbox.Defender.OnBlock(Attack);
        Attack.Attacker.OnBlock(Attack, hurtbox.Defender);
      } else {
        hurtbox.Defender.OnHit(Attack);
        Attack.Attacker.OnHit(Attack, hurtbox.Defender);
      }
    }
  }
}