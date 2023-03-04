using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class WeaponTrail : MonoBehaviour {
  [SerializeField] MeshFilter MeshFilter;
  [SerializeField] MeshRenderer MeshRenderer;
  [SerializeField] Transform T0;
  [SerializeField] Transform T1;
  [SerializeField] float MaxTrailDistance = 5;
  [SerializeField] float MaxSpawnSpeed = 5;
  [SerializeField] Timeval SegmentDuration = Timeval.FromMillis(100);

  public bool Emitting = true;

  Vector3 P0;
  Vector3 P1;
  List<Vector3> Trail0 = new();
  List<Vector3> Trail1 = new();
  List<float> SpawnSpeeds0 = new();
  List<float> SpawnSpeeds1 = new();
  List<float> DeathTimes = new();
  float[] Distances0 = new float[0];
  float[] Distances1 = new float[0];
  Mesh Mesh;

  void OnEnable() {
    Trail0 = new();
    Trail1 = new();
    DeathTimes = new();
    P0 = T0.position;
    P1 = T1.position;
    Trail0.Add(T0.position);
    Trail1.Add(T1.position);
    DeathTimes.Add(Time.time + SegmentDuration.Seconds);
    SpawnSpeeds0.Add(0);
    SpawnSpeeds1.Add(0);
    Mesh = new();
    // Steve - intention here is to create a copy of the sharedMaterial
    // but then to refer to this material by "sharedMaterial". If this works
    // as intended, it should allow materials to be modified in edit mode
    // MP - This seems to be causing some weirdness with the material randomly
    // resetting to null in the editor.
    //MeshRenderer.material = new Material(MeshRenderer.sharedMaterial);
    MeshFilter.mesh = Mesh;
  }

  void OnDisable() {
    Trail0.Clear();
    Trail1.Clear();
    DeathTimes.Clear();
    SpawnSpeeds0.Clear();
    SpawnSpeeds1.Clear();
    Mesh.Clear();
  }

  void LateUpdate() {
    if (Emitting)
      InterpolatePositions();
    RemoveOldPositions();
    ComputeDistances();
    RenderToMesh();
  }

  void RemoveOldPositions() {
    var cutCount = 0;
    for (var i = 0; i < DeathTimes.Count; i++) {
      var time = DeathTimes[i];
      if (Time.time >= time) {
        cutCount++;
      } else {
        break;
      }
    }
    Trail0.RemoveRange(0, cutCount);
    Trail1.RemoveRange(0, cutCount);
    DeathTimes.RemoveRange(0, cutCount);
    SpawnSpeeds0.RemoveRange(0, cutCount);
    SpawnSpeeds1.RemoveRange(0, cutCount);
  }

  void RecordPosition(Vector3 p0, Vector3 p1) {
    Trail0.Add(p0);
    Trail1.Add(p1);
    DeathTimes.Add(Time.time + SegmentDuration.Seconds);
    SpawnSpeeds0.Add((Vector3.Distance(p0, P0))/SegmentDuration.Seconds);
    SpawnSpeeds1.Add((Vector3.Distance(p1, P1))/SegmentDuration.Seconds);
    P0 = p0;
    P1 = p1;
  }

  // The basic idea here is to assume the weapon is swinging in a circular arc around an anchor point.
  // We start with an old and a new line segment for the weapon position in world space. Process:
  // 1. Find the anchor point based on the intersection of the old and new line segments in the XZ-plane.
  // 2. Rotate the old line segment around the anchor point towards the new line segment.
  // 3. Record positions as we rotate until we reach the new line segment.
  void InterpolatePositions() {
    var newP0 = T0.position;
    var newP1 = T1.position;
    var anchor = FindAnchor(P0, P1, newP0, newP1);
    var delta0 = P0 - anchor;
    var delta1 = P1 - anchor;
    var newDelta0 = newP0 - anchor;
    var newDelta1 = newP1 - anchor;
    const float epsilon = .1f;
    const float minTurnSpeed = .1f;
    const float magSpeed = .1f;
    const int maxIterations = 8;
    var angle = Vector3.Angle(delta1, newDelta1) * Mathf.Deg2Rad;
    var turnSpeed = Mathf.Max(minTurnSpeed, angle / maxIterations);
    for (int i = 0; i < maxIterations; i++) {
      delta0 = Vector3.RotateTowards(delta0, newDelta0, turnSpeed, magSpeed);
      delta1 = Vector3.RotateTowards(delta1, newDelta1, turnSpeed, magSpeed);
      if ((newDelta1 - delta1).sqrMagnitude < epsilon.Sqr())
        break;
      var curP0 = anchor + delta0;
      var curP1 = anchor + delta1;
      RecordPosition(curP0, curP1);
    }
    RecordPosition(newP0, newP1);
  }

  Vector3 FindAnchor(Vector3 p0, Vector3 p1, Vector3 newP0, Vector3 newP1) {
    var ix = LineIntersection(p0.XZ2(), p1.XZ2(), newP0.XZ2(), newP1.XZ2()) ?? newP0.XZ2();
    return new(ix.x, newP0.y, ix.y);
  }
  public Vector2? LineIntersection(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2) {
    float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
    if (tmp == 0)
      return null;

    float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
    return new Vector2(
      B1.x + (B2.x - B1.x) * mu,
      B1.y + (B2.y - B1.y) * mu
    );
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
    if (Distances0.Length > 0) {
      Distances0[Distances0.Length-1] = 0;
      Distances1[Distances1.Length-1] = 0;
    }
  }

  void RenderToMesh() {
    List<Vector3> Vertices = new();
    List<Vector4> UVs = new();
    List<int> Triangles = new();
    for (var i = 0; i < Trail0.Count; i++) {
      Vertices.Add(transform.InverseTransformPoint(Trail0[i]));
      Vertices.Add(transform.InverseTransformPoint(Trail1[i]));
    }
    for (var i = 0; i < DeathTimes.Count; i++) {
      var start = DeathTimes[i]-SegmentDuration.Seconds;
      var end = DeathTimes[i];
      var normalizedAge = Mathf.InverseLerp(start, end, Time.time);
      var age = normalizedAge*SegmentDuration.Seconds;
      var falloff = 1;
      var averageDistance = (Distances0[i]+Distances1[i])/2;
      var averageSpawnSpeed = (SpawnSpeeds0[i]+SpawnSpeeds1[i])/2;
      UVs.Add(new Vector4(averageDistance, age, averageSpawnSpeed, falloff));
      UVs.Add(new Vector4(averageDistance, age, averageSpawnSpeed, falloff));
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
    MeshRenderer.sharedMaterial.SetFloat("_MaxVelocity", MaxSpawnSpeed);
    MeshRenderer.sharedMaterial.SetFloat("_MaxDistance", MaxTrailDistance);
    MeshRenderer.sharedMaterial.SetFloat("_MaxAge", SegmentDuration.Seconds);
  }

#if false
  void OnDrawGizmos() {
    Gizmos.color = Color.yellow;
    for (int i = 0; i < Trail0.Count; i++) {
      Gizmos.DrawLine(Trail0[i], Trail1[i]);
    }
  }
#endif
}