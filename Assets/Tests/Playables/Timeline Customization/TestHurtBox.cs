using UnityEngine;

public struct MeleeContact {
  public readonly Hitbox Hitbox;
  public readonly TestHurtBox Hurtbox;
  public MeleeContact(Hitbox hitbox, TestHurtBox hurtbox) {
    Hitbox = hitbox;
    Hurtbox = hurtbox;
  }
}

public class TestHurtBox : MonoBehaviour {
  public GameObject Owner;

  void OnTriggerEnter(Collider collider) {
    if (collider.TryGetComponent(out Hitbox hitbox)) {
      Owner?.SendMessage("OnContact", new MeleeContact(hitbox, this));
    }
  }
}