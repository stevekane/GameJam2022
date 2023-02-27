using UnityEngine;

public class TestHurtBox : MonoBehaviour {
  public GameObject Owner;

  void OnTriggerEnter(Collider collider) {
    const SendMessageOptions OPTIONS = SendMessageOptions.DontRequireReceiver;
    var hitbox = collider.GetComponent<Hitbox>();
    if (hitbox) {
      hitbox.Owner?.SendMessage("OnHit", this, OPTIONS);
      Owner?.SendMessage("OnHurt", hitbox, OPTIONS);
    }
  }
}