
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FocusAllCharacters : MonoBehaviour {
  void Update() {
    var targetGroup = GetComponent<Unity.Cinemachine.CinemachineTargetGroup>();
    var targets = FindObjectsOfType<AbilityManager>();
    targetGroup.m_Targets =
      targets.Select(t => new Unity.Cinemachine.CinemachineTargetGroup.Target() {
        Object = t.transform,
        Weight = 1
      })
      .ToArray();
  }
}