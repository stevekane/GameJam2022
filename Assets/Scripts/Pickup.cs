using UnityEngine;

public class Pickup : MonoBehaviour {
  void OnTriggerEnter(Collider c) {
    Debug.Log($"{c} picked me up");
  }
}
