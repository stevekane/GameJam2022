using UnityEngine;

namespace Archero {
  public class PlayerManager : MonoBehaviour {
    public static PlayerManager Instance;

    public Player Player;

    void Awake() {
      Instance = this;
    }

    void OnDestroy() {
      Instance = null;
    }
  }
}