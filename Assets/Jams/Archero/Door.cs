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
      GameManager.Instance.OnLevelComplete();
    }

    void Start() {
      DoorTrigger.OnTriggerEnterSource.Listen(OnTriggerEnter);
    }
  }
}