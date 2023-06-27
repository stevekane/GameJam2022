using UnityEngine;

public class Player : MonoBehaviour {
  public static Player Get() => FindObjectOfType<Player>();
  public bool Dead = false;

  void OnDeath(Vector3 normal) {
    Dead = true;
  }

  void Awake() {
    PlayerManager.Instance?.RegisterPlayer(this);
  }

  void OnDestroy() {
    PlayerManager.Instance?.UnregisterPlayer(this);
  }
}