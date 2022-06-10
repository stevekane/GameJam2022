using UnityEngine;

public class TargetableTriggerHandler : MonoBehaviour {
  public Hero Hero;

  void OnTriggerEnter(Collider other) {
    Hero.Enter(other.gameObject);
  }

  void OnTriggerStay(Collider other) {
    Hero.Stay(other.gameObject);
  }

  void OnTriggerExit(Collider other) {
    Hero.Exit(other.gameObject);
  }
}