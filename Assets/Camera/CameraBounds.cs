using UnityEngine;

// Collider that adds/removes CameraSubjects from the CameraManager's TargetGroup
public class CameraBounds: MonoBehaviour {
  static void Enable(Collider c) => c.enabled = true;
  static void Disable(Collider c) => c.enabled = false;

  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out CameraSubject subject)) {
      subject.Colliders.ForEach(Enable);
      CameraManager.Instance.TargetGroup.AddMember(subject.transform, 1, 1);
    }
  }

  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out CameraSubject subject)) {
      subject.Colliders.ForEach(Disable);
      CameraManager.Instance.TargetGroup.RemoveMember(subject.transform);
    }
  }
}