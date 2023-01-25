using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteInEditMode]
public class ControllableMeshStrip : MonoBehaviour {
  [SerializeField] int SampleCount = 10;
  [SerializeField] SplineContainer SplineContainer;
  [SerializeField] Material Material;
  [SerializeField] float Width = 1;
  [SerializeField] float SegmentDeltaTime = 1/60;
  [SerializeField] float SegmentAge = 0;

  Mesh Mesh;

  void Start() {
    Mesh = new();
  }

  void LateUpdate() {
    // Record points
    var Trail0 = new List<Vector3>();
    var Trail1 = new List<Vector3>();
    for (var i = 0; i <= SampleCount; i++) {
      var interpolant = (float)i/(float)SampleCount;
      SplineContainer.Spline.Evaluate(interpolant, out var position, out var tangent, out var up);
      position = transform.TransformPoint(position);
      tangent = math.normalize(tangent);
      var axis = math.normalize(math.cross(tangent, up));
      var p0 = position + Width/2 * axis;
      var p1 = position - Width/2 * axis;
      Trail0.Add(p0);
      Trail1.Add(p1);
    }

    // Record distances
    float d0 = 0;
    // float d1 = 0;
    var Distances0 = new float[Trail0.Count];
    var Distances1 = new float[Trail1.Count];
    for (var i = Trail0.Count-2; i >= 0; i--) {
      var distance0 = Vector3.Distance(Trail0[i], Trail0[i+1]);
      var distance1 = Vector3.Distance(Trail1[i], Trail1[i+1]);
      var distanceAverage = (distance0+distance1)/2;
      d0 += distanceAverage;
      // d1 += distanceAverage;
      Distances0[i] = d0;
      Distances1[i] = d0;
      // d0 += Vector3.Distance(Trail0[i], Trail0[i+1]);
      // Distances0[i] = d0;
      // d1 += Vector3.Distance(Trail1[i], Trail1[i+1]);
      // Distances1[i] = d1;
    }
    Distances0[Distances0.Length-1] = 0;
    Distances1[Distances1.Length-1] = 0;

    // Write things to mesh
    List<Vector3> Vertices = new();
    List<Vector4> UVs = new();
    List<int> Triangles = new();
    for (var i = 0; i < Trail0.Count; i++) {
      Vertices.Add(Trail0[i]);
      Vertices.Add(Trail1[i]);
    }
    for (var i = 0; i < Distances0.Length; i++) {
      var age = SegmentAge + i*SegmentDeltaTime;
      var spawnVelocity = 10;
      var falloff = 1;
      UVs.Add(new Vector4(Distances0[i], age, spawnVelocity, falloff));
      UVs.Add(new Vector4(Distances1[i], age, spawnVelocity, 0));
    }
    var total = (Vertices.Count - 2) / 2;
    for (var i = 0; i < total; i++) {
      Triangles.Add(i*2+0);
      Triangles.Add(i*2+2);
      Triangles.Add(i*2+3);
      Triangles.Add(i*2+0);
      Triangles.Add(i*2+3);
      Triangles.Add(i*2+1);
    }
    Mesh.Clear();
    Mesh.SetVertices(Vertices);
    Mesh.SetUVs(0, UVs);
    Mesh.SetTriangles(Triangles, 0);
    Graphics.DrawMesh(Mesh, Vector3.zero, Quaternion.identity, Material, 0);
  }
}