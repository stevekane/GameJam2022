using UnityEngine;

public struct MeleeContact {
  public readonly HitboxSteve Hitbox;
  public readonly TestHurtBox Hurtbox;
  public MeleeContact(HitboxSteve hitbox, TestHurtBox hurtbox) {
    Hitbox = hitbox;
    Hurtbox = hurtbox;
  }
}

public class TestHurtBox : MonoBehaviour {
  public GameObject Owner;

  void OnTriggerEnter(Collider collider) {
    if (collider.TryGetComponent(out HitboxSteve hitbox)) {
      if (!hitbox.Targets.Contains(gameObject)) {
        hitbox.Targets.Add(gameObject);
        Owner?.SendMessage("OnContact", new MeleeContact(hitbox, this));
      }
    }
  }
}