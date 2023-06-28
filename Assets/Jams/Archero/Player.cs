using UnityEngine;

namespace Archero {
  public class Player : MonoBehaviour {
    public static Player Instance;

    void Awake() {
      Instance = this;
    }

    void OnDestroy() {
      Instance = null;
    }
  }
}