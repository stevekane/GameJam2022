using Cinemachine;
using UnityEngine;

// Reference to TargetGroup. Subjects are added/removed primarily from CameraTargetBox
public class CameraManager : MonoBehaviour {
  public static CameraManager Instance;

  [SerializeField] string GhostTagName = "CameraGhost";
  [SerializeField] float WeightPerSecond = 1;
  [SerializeField] CinemachineTargetGroup TargetGroup;

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
    var targetCount = TargetGroup.m_Targets.Length;
    for (var i = targetCount-1; i >= 0; i--) {
      var target = TargetGroup.m_Targets[i];
      if (target.target == null) {
        TargetGroup.RemoveMember(target.target);
      } else {
        if (target.target.CompareTag(GhostTagName)) {
          if (target.weight <= 0) {
            TargetGroup.RemoveMember(target.target);
          } else {
            target.weight = Mathf.MoveTowards(target.weight, 0, Time.deltaTime * WeightPerSecond);
            TargetGroup.m_Targets[i] = target;
          }
        } else {
          target.weight = Mathf.MoveTowards(target.weight, 1, Time.deltaTime * WeightPerSecond);
          TargetGroup.m_Targets[i] = target;
        }
      }
    }
  }
}