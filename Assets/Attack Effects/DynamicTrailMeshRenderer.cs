using System.Collections.Generic;
using UnityEngine;

public class DynamicTrailMeshRenderer : MonoBehaviour {
  [SerializeField] Transform T0;
  [SerializeField] Transform T1;
  [SerializeField] Material Material;
  [SerializeField] int MaxSegments = 256;
  [SerializeField] float MaxTrailDistance = 5;

  List<Vector3> Trail0 = new();
  List<Vector3> Trail1 = new();
  float[] Distances0 = new float[0];
  float[] Distances1 = new float[0];
  GameObject Root;
  MeshFilter MeshFilter;
  MeshRenderer MeshRenderer;

  void Start() {
    Trail0 = new();
    Trail1 = new();
    Trail0.Add(T0.position);
    Trail1.Add(T1.position);
    Root = new GameObject("Dynamic Trail");
    Root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    MeshFilter = Root.AddComponent<MeshFilter>();
    MeshFilter.mesh = new();
    MeshRenderer = Root.AddComponent<MeshRenderer>();
    MeshRenderer.material = Material;
  }

  void OnDestroy() {
    Destroy(Root);
  }

  void LateUpdate() {
    RecordPositions();
    RemoveOldPositions();
    ComputeDistances();
    RenderToMesh();
  }

  void RemoveOldPositions() {
    if (Trail0.Count > MaxSegments) {
      Trail0.RemoveAt(0);
      Trail1.RemoveAt(0);
    }
  }

  void RecordPositions() {
    Trail0.Add(T0.position);
    Trail1.Add(T1.position);
  }

  // TODO: Allocation here is stupid.. fucking programming
  void ComputeDistances() {
    // Distances measured from the head of the trail
    float d0 = 0;
    float d1 = 0;
    Distances0 = new float[Trail0.Count];
    Distances1 = new float[Trail1.Count];
    for (var i = Trail0.Count-2; i >= 0; i--) {
      d0 += Vector3.Distance(Trail0[i], Trail0[i+1]);
      Distances0[i] = d0;
      d1 += Vector3.Distance(Trail1[i], Trail1[i+1]);
      Distances1[i] = d1;
    }
    Distances0[Distances0.Length-1] = 0;
    Distances1[Distances1.Length-1] = 0;
  }

  void RenderToMesh() {
    List<Vector3> Vertices = new();
    List<Vector2> UVs = new();
    List<int> Triangles = new();
    for (var i = 0; i < Trail0.Count; i++) {
      Vertices.Add(Trail0[i]);
      Vertices.Add(Trail1[i]);
    }
    for (var i = 0; i < Distances0.Length; i++) {
      UVs.Add(new Vector2(Distances0[i], 0));
      UVs.Add(new Vector2(Distances1[i], 1));
    }
    // triangles are
    var total = (Vertices.Count - 2) / 2;
    for (var i = 0; i < total; i++) {
      Triangles.Add(i*2+0);
      Triangles.Add(i*2+2);
      Triangles.Add(i*2+3);
      Triangles.Add(i*2+0);
      Triangles.Add(i*2+3);
      Triangles.Add(i*2+1);
    }
    MeshFilter.mesh.Clear();
    MeshFilter.mesh.SetVertices(Vertices);
    MeshFilter.mesh.SetUVs(0, UVs);
    MeshFilter.mesh.SetTriangles(Triangles, 0);
  }

  void OnDrawGizmosSelected() {
    for (var i = 1; i < Trail0.Count; i++) {
      var color0 = Color.Lerp(Color.black, Color.white, 1-Mathf.Clamp01(Mathf.InverseLerp(0, MaxTrailDistance, Distances0[i])));
      var color1 = Color.Lerp(Color.black, Color.white, 1-Mathf.Clamp01(Mathf.InverseLerp(0, MaxTrailDistance, Distances1[i])));
      var connectorColor = Color.Lerp(color0, color1, .5f);
      Gizmos.color = color0;
      Gizmos.DrawLine(Trail0[i-1], Trail0[i]);
      Gizmos.color = color1;
      Gizmos.DrawLine(Trail1[i-1], Trail1[i]);
      Gizmos.color = connectorColor;
      Gizmos.DrawLine(Trail0[i], Trail1[i]);
    }
  }
}