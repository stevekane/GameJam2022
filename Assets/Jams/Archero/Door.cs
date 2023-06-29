using UnityEngine;

namespace Archero {
  public class Door : MonoBehaviour {
    [SerializeField] Animator Animator;
    [SerializeField] TriggerEvent DoorTrigger;

    [ContextMenu("Open")]
    public void Open() {
      Animator.SetTrigger("Open");
    }

    void OnTriggerEnter(Collider other) {
      GameManager.Instance.OnLevelComplete();
    }

    void Start() {
      DoorTrigger.OnTriggerEnterSource.Listen(OnTriggerEnter);
    }
  }
}