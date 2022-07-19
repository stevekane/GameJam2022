using UnityEngine;

public class Hitbox : MonoBehaviour {
  public Attacker Attacker { get; set; }
  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      if (hurtbox.Defender.IsParrying) {
        hurtbox.Defender.OnParry(Attacker);
        Attacker.OnParry(hurtbox.Defender);
      } else if (hurtbox.Defender.IsBlocking) {
        hurtbox.Defender.OnBlock(Attacker);
        Attacker.OnBlock(hurtbox.Defender);
      } else {
        hurtbox.Defender.OnHit(Attacker);
        Attacker.OnHit(hurtbox.Defender);
      }
    }
  }
}