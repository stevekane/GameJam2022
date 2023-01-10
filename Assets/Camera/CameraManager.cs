using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

// Reference to TargetGroup. Subjects are added/removed primarily from CameraTargetBox
public class CameraManager : MonoBehaviour {
  public static CameraManager Instance;

  public CinemachineTargetGroup TargetGroup;
}