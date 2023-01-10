using UnityEngine;

// When active, Detects overlap with CameraSubjectInterest
public class CameraSubjectInterestBounds : MonoBehaviour {
  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out CameraSubjectInterest interest)) {
      if (CameraManager.Instance.TargetGroup.FindMember(interest.transform) < 0) {
        CameraManager.Instance.TargetGroup.AddMember(interest.transform, 1, 1);
      }
    }
  }

  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out CameraSubjectInterest interest)) {
      CameraManager.Instance.TargetGroup.RemoveMember(interest.transform);
    }
  }
}