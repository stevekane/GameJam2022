using Cinemachine;
using UnityEngine;

// Reference to TargetGroup. Subjects are added/removed primarily from CameraTargetBox
public class CameraManager : MonoBehaviour {
  public static CameraManager Instance;

  public CinemachineTargetGroup TargetGroup;
}