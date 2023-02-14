using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorLabel : MonoBehaviour {
  [SerializeField] string Label = "";
  [SerializeField] float Height = 1;
  #if UNITY_EDITOR
  void OnDrawGizmos() {
    Handles.Label(transform.position + Height*Vector3.up, Label);
  }
  #endif
}