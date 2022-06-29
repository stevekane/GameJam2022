using UnityEngine;
using UnityEngine.Events;

public class Hitbox : MonoBehaviour {
  [SerializeField]
  UnityEvent<Hurtbox> OnHit;

  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Hurtbox hurtbox)) {
      OnHit.Invoke(hurtbox);
    }
  }
}