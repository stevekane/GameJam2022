
using UnityEngine;

public class CameraManager : MonoBehaviour {
  public static CameraManager Instance;

  [SerializeField] string GhostTagName = "CameraGhost";
  [SerializeField] float WeightPerSecond = 1;
  [SerializeField] Unity.Cinemachine.CinemachineTargetGroup TargetGroup;

  public void AddTarget(CameraSubject subject) {
    if (TargetGroup.FindMember(subject.transform) < 0) {
      TargetGroup.AddMember(subject.transform, 0, subject.Radius);
    }
  }

  public void RemoveTarget(CameraSubject subject, bool shouldSpawnGhost) {
    TargetGroup.RemoveMember(subject.transform);
    if (shouldSpawnGhost) {
      var ghost = new GameObject("Camera Ghost");
      ghost.tag = GhostTagName;
      ghost.transform.SetPositionAndRotation(subject.transform.position, subject.transform.rotation);
      ghost.transform.localScale = subject.transform.localScale;
      TargetGroup.AddMember(ghost.transform, 1, subject.Radius);
    }
  }

  void Update() {
    var targetCount = TargetGroup.Targets.Count;
    for (var i = targetCount-1; i >= 0; i--) {
      var target = TargetGroup.Targets[i];
      if (target.Object == null) {
        TargetGroup.RemoveMember(target.Object);
      } else {
        if (target.Object.CompareTag(GhostTagName)) {
          if (target.Weight <= 0) {
            TargetGroup.RemoveMember(target.Object);
          } else {
            target.Weight = Mathf.MoveTowards(target.Weight, 0, Time.deltaTime * WeightPerSecond);
            TargetGroup.Targets[i] = target;
          }
        } else {
          target.Weight = Mathf.MoveTowards(target.Weight, 1, Time.deltaTime * WeightPerSecond);
          TargetGroup.Targets[i] = target;
        }
      }
    }
  }
}