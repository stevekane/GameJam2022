using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class WeaponTrail : MonoBehaviour {
  [SerializeField] LocalTime LocalTime;
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
    DeathTimes.Add(LocalTime.Time + SegmentDuration.Seconds);
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
    P0 = T0.position;
    P1 = T1.position;
  }

  void RemoveOldPositions() {
    var cutCount = 0;
    for (var i = 0; i < DeathTimes.Count; i++) {
      var time = DeathTimes[i];
      var currentTime = LocalTime == null ? Time.time : LocalTime.Time;
      if (currentTime >= time) {
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
    var currentTime = LocalTime == null ? Time.time : LocalTime.Time;
    DeathTimes.Add(currentTime + SegmentDuration.Seconds);
    SpawnSpeeds0.Add((Vector3.Distance(p0, P0))/SegmentDuration.Seconds);
    SpawnSpeeds1.Add((Vector3.Distance(p1, P1))/SegmentDuration.Seconds);
    P0 = p0;
    P1 = p1;
  }

  // Simple interpolation between 2 line segments. Use linear interpolation of the inner point's position (p0),
  // and of the line segment's rotation angle (segment to newSegment), to find the outer point's interpolated positions.
  public int MaxIterations = 8;
  void InterpolatePositions() {
    var newP0 = T0.position;
    var newP1 = T1.position;
    var curP0 = P0;
    var segment = P1 - P0;
    var newSegment = newP1 - newP0;
    const float epsilon = .1f;
    const float magSpeed = .1f;
    const float minTurnSpeed = .1f;
    var angle = Vector3.Angle(segment, newSegment) * Mathf.Deg2Rad;
    var turnSpeed = Mathf.Max(minTurnSpeed, angle / MaxIterations);
    for (int i = 0; i < MaxIterations; i++) {
      curP0 = Vector3.MoveTowards(curP0, newP0, magSpeed);
      segment = Vector3.RotateTowards(segment, newSegment, turnSpeed, magSpeed);
      var curP1 = curP0 + segment;
      if ((curP1 - newP1).sqrMagnitude < epsilon.Sqr())
        break;
      RecordPosition(curP0, curP1);
    }
    RecordPosition(newP0, newP1);
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
    if (!Mesh) return;
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
      var currentTime = LocalTime == null ? Time.time : LocalTime.Time;
      var normalizedAge = Mathf.InverseLerp(start, end, currentTime);
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

#if true
  void OnDrawGizmos() {
    Gizmos.color = Color.yellow;
    for (int i = 0; i < Trail0.Count; i++) {
      Gizmos.DrawLine(Trail0[i], Trail1[i]);
    }
  }
#endif
}