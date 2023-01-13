using Cinemachine;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FocusAllCharacters : MonoBehaviour {
  void Update() {
    var targetGroup = GetComponent<CinemachineTargetGroup>();
    var targets = FindObjectsOfType<AbilityManager>();
    targetGroup.m_Targets =
      targets.Select(t => new CinemachineTargetGroup.Target() {
        target = t.transform,
        weight = 1
      })
      .ToArray();
  }
}