using UnityEngine;
using UnityEngine.Events;

public class Hurtbox : MonoBehaviour {
  public UnityEvent<GameObject> HitEvent;

  private void OnTriggerEnter(Collider other) {
    HitEvent.Invoke(other.gameObject);
  }
}
