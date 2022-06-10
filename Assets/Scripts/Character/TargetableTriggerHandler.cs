using UnityEngine;

public class TargetableTriggerHandler : MonoBehaviour {
  public Hero Hero;

  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Targetable targetable)) {
      Hero.Contact(targetable);
    }
  }
}