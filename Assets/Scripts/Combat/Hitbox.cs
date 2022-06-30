using UnityEngine;
using UnityEngine.Events;

public class Hitbox : MonoBehaviour {
  public Collider Collider;

  [SerializeField]
  UnityEvent<Hurtbox> OnHit;

  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      OnHit.Invoke(hurtbox);
    }
  }
}