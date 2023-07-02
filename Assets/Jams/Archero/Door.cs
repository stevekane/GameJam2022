using UnityEngine;
using TMPro;

namespace Archero {
  public class Door : MonoBehaviour {
    [SerializeField] Animator Animator;
    [SerializeField] TriggerEvent DoorTrigger;
    [SerializeField] bool OpenOnAwake;
    [SerializeField] TextMeshPro RoomNumber;

    void Awake() {
      if (OpenOnAwake)
        Open();
      RoomNumber.text = GameManager.Instance.CurrentRoom.ToString();
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