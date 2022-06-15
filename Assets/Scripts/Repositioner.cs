using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Repositioner : MonoBehaviour {
  public void MoveChildren() {
    var scale = transform.localScale;
    transform.localScale = new Vector3(1, 1, 1);
    foreach (Transform child in transform) {
      child.position = Vector3.Scale(child.position, scale);
    }
  }
}

[CustomEditor(typeof(Repositioner))]
[CanEditMultipleObjects]
public class RepositionerEditor : Editor {
  void OnEnable() {
  }

  public override void OnInspectorGUI() {
    DrawDefaultInspector();

    if (GUILayout.Button("Reposition Children")) {
      var r = (Repositioner)target;
      r.MoveChildren();
      Debug.Log($"transform is {r.transform.localScale}");
    }
  }
}