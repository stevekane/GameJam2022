using UnityEngine;

namespace Archero {
  public class Door : MonoBehaviour {
    [SerializeField] Animator Animator;
    [SerializeField] TriggerEvent DoorTrigger;
    [SerializeField] bool OpenOnAwake;

    void Awake() {
      if (OpenOnAwake)
        Open();
    }

    [ContextMenu("Open")]
    public void Open() {
      Animator.SetTrigger("Open");
    }

    void OnTriggerEnter(Collider other) {
      GameManager.Instance.OnRoomExited();
    }

    void Start() {
      DoorTrigger.OnTriggerEnterSource.Listen(OnTriggerEnter);
    }
  }
}