using UnityEngine;

public class TargetableTriggerHandler : MonoBehaviour {
  public Hero Hero;

  void OnTriggerEnter(Collider other) {
    if (other.TryGetComponent(out Targetable targetable)) {
      Debug.Log($"Hero touched {other}");
      Hero.Contact(targetable);
    }
  }
}