using UnityEngine;

namespace Archero {
  public class LevelManager : MonoBehaviour {
    [SerializeField] MobManager MobManager;
    void Awake() {
      MobManager.Instance = MobManager;
    }
    void OnDestroy() {
      MobManager.Instance = null;
    }
  }
}