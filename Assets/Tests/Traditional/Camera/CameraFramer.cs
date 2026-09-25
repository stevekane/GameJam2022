
using UnityEngine;

namespace Traditional {
    public class CameraFramer : MonoBehaviour {
    [SerializeField] Unity.Cinemachine.CinemachineTargetGroup Group;
    [SerializeField] string Tag;

    void OnTriggerEnter(Collider c) {
      if (c.CompareTag(Tag)) {
        Group.AddMember(c.transform, 1, 1);
      }
    }

    void OnTriggerExit(Collider c) {
      if (c.CompareTag(Tag)) {
        Group.RemoveMember(c.transform);
      }
    }
  }
}