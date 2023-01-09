using UnityEngine;

/*
Add to GameObjects you wish to be kept in-view by the camera
When this subject is added, it should turn on its own collider
This collider checks for overlaps that contain CameraSubjectInterest
and adds them to the CameraManager's TargetGroup
*/
public class CameraSubject : MonoBehaviour {
  public Collider[] Colliders;

  void OnTriggerEnter(Collider c) {
    if (c.TryGetComponent(out CameraSubjectInterest interest)) {
      CameraManager.Instance.TargetGroup.AddMember(interest.transform, 1, 1);
    }
  }
  void OnTriggerExit(Collider c) {
    if (c.TryGetComponent(out CameraSubjectInterest interest)) {
      CameraManager.Instance.TargetGroup.RemoveMember(interest.transform);
    }
  }
}