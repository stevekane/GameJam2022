using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Repositioner : MonoBehaviour {
  public float ScaleFactor = 1;

  public void MoveChildren() {
    foreach (Transform child in transform) {
      child.position *= ScaleFactor;
    }
  }
}

[CustomEditor(typeof(Repositioner))]
[CanEditMultipleObjects]
public class RepositionerEditor : Editor {
  public override void OnInspectorGUI() {
    DrawDefaultInspector();

    if (GUILayout.Button("Reposition Children")) {
      var r = (Repositioner)target;
      r.MoveChildren();
    }
  }
}