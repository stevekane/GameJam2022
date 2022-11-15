using UnityEngine;

public class InstantKill : MonoBehaviour {
  void OnTriggerEnter2D(Collider2D c) {
    if (c.GetComponent<HollowKnight>()) {
      Destroy(c.gameObject);
    }
  }
}
