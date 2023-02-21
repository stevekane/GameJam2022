using UnityEngine;

public class TestHurtBox : MonoBehaviour {
  void OnTriggerEnter(Collider collider) {
    Debug.Log("You got hit");
  }
}