using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archero {
  public class Door : MonoBehaviour {
    public GameObject DoorObject;
    public TriggerEvent DoorTrigger;

    [ContextMenu("Open")]
    public void Open() {
      DoorObject.Destroy();
    }

    void OnTriggerEnter(Collider other) {
      Debug.Log($"Door {other.gameObject} in level {GameManager.Instance.CurrentLevel}");
      GameManager.Instance.OnLevelComplete();
    }

    void Start() {
      DoorTrigger.OnTriggerEnterSource.Listen(OnTriggerEnter);
    }
  }
}