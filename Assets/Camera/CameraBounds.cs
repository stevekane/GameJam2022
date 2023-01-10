using UnityEngine;

// Collider that adds/removes CameraSubjects from the CameraManager's TargetGroup
public class CameraBounds: MonoBehaviour {
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out CameraSubject subject)) {
      if (CameraManager.Instance.TargetGroup.FindMember(subject.transform) < 0) {
        subject.InterestBoundsCollider.enabled = true;
        CameraManager.Instance.TargetGroup.AddMember(subject.transform, 1, 1);
      }
    }
  }

  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out CameraSubject subject)) {
        subject.InterestBoundsCollider.enabled = false;
      CameraManager.Instance.TargetGroup.RemoveMember(subject.transform);
    }
  }
}