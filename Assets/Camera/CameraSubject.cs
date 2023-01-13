using UnityEngine;

/*
Add to GameObjects you wish to be kept in-view by the camera.

When targeted, this turns on CameraSubjectInterestBounds objects
which in turn may detect CameraSubjectInterests and target them
with the CameraManager.
*/
public class CameraSubject : MonoBehaviour {
  public Collider InterestBoundsCollider;
  public float Radius = 3;

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